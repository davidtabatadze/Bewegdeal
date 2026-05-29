using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Bewegdeal.Hubs;
using Bewegdeal.Models;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Bewegdeal.Controllers;

[RequireLogin]
public class ChatController(
    UserService userService,
    IUserRepository userRepository,
    IRequestRepository requestRepository,
    IChatRepository chatRepository,
    IHubContext<ChatHub> hubContext) : XBaseController(userRepository)
{

    [HttpGet]
    public async Task<IActionResult> Visibility(string requestNumber)
    {
        var user = await GetUser(null, true, null);

        var request = await requestRepository.Get(requestNumber);

        var chat = await chatRepository.GetActive(request?.Id ?? 0);

        return Json(new
        {
            mode =
                user?.Role == UserRoleEnum.Company &&
                request?.Status == RequestStatusEnum.Pending
                    ? ChatModeEnum.Initiate :

                request?.Status == RequestStatusEnum.Negotiation &&
                (chat?.CompanyId == user?.Id || chat?.CustomerId == user?.Id)
                    ? ChatModeEnum.Active :

                    ChatModeEnum.None
        });
    }

    [HttpPost]
    public async Task<IActionResult> Initiate(string requestNumber)
    {
        var user = await GetUser(roles: [UserRoleEnum.Company], active: true);
        if (user is null)
        {
            return Json(new { success = false, error = "Access denied." });
        }

        var request = await requestRepository.Get(requestNumber);
        if (request is null || request.Status != RequestStatusEnum.Pending)
        {
            return Json(new { success = false, error = "This request is no longer available." });
        }

        // create the active chat
        var chat = await chatRepository.Create(new ChatEntity
        {
            Key = Guid.NewGuid().ToString("N"),
            RequestId = request.Id,
            CustomerId = request.RequesterId,
            CompanyId = user.Id,
            Status = ChatStatusEnum.Active,
            CreateDate = DateTime.UtcNow
        });

        // transition request to negotiation
        request.Status = RequestStatusEnum.Negotiation;
        await requestRepository.Update(request);

        // load customer info so the company can see who they're chatting with
        var customer = await UserRepository.Get(new UserFilter { Id = request.RequesterId });
        var customerAvatar = await userService.GetAvatar(customer);

        return Json(new
        {
            success = true,
            chatKey = chat.Key,
            otherPartyName = customerAvatar.Name,
            otherPartyInitials = customerAvatar.Initials,
            otherPartyPictureUrl = customerAvatar.Url
        });
    }


    /// <summary>
    /// Server-rendered conversation partial — handles both initiate and active modes.
    /// Called when the offcanvas opens; returns HTML directly.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Conversation(string requestNumber)
    {
        var user = await GetUser(null, true, null);
        if (user is null) { return Content(""); }

        var request = await requestRepository.Get(requestNumber);
        if (request is null) { return Content(""); }

        var activeChat = await chatRepository.GetActive(request.Id);

        var mode = ChatModeEnum.None;
        if (user.Role == UserRoleEnum.Company && request.Status == RequestStatusEnum.Pending)
        {
            mode = ChatModeEnum.Initiate;
        }
        else if (request.Status == RequestStatusEnum.Negotiation &&
                 (activeChat?.CompanyId == user.Id || activeChat?.CustomerId == user.Id))
        {
            mode = ChatModeEnum.Active;
        }

        if (mode == ChatModeEnum.None) { return Content(""); }

        var viewerAvatar = await userService.GetAvatar(user);

        long otherPartyId = activeChat is not null
            ? (user.Role == UserRoleEnum.Customer ? activeChat.CompanyId : activeChat.CustomerId)
            : request.RequesterId;

        var otherParty = await UserRepository.Get(new UserFilter { Id = otherPartyId });
        var otherPartyAvatar = await userService.GetAvatar(otherParty);

        var messages = activeChat is not null
            ? await chatRepository.LoadMessages(activeChat.Id)
            : [];

        return PartialView("Conversation", new ChatHistoryViewModel
        {
            Mode = mode,
            ChatKey = activeChat?.Key ?? "",
            ViewerId = user.Id,
            ViewerInitials = viewerAvatar.Initials,
            ViewerPictureUrl = viewerAvatar.Url,
            OtherPartyName = otherPartyAvatar.Name,
            OtherPartyInitials = otherPartyAvatar.Initials,
            OtherPartyPictureUrl = otherPartyAvatar.Url,
            Messages = messages
        });
    }



    [HttpPost]
    public async Task<IActionResult> Cancel(string requestNumber)
    {
        var user = await GetUser(null, true, null);
        if (user is null) { return Json(new { success = false }); }

        var request = await requestRepository.Get(requestNumber);
        if (request is null) { return Json(new { success = false }); }

        var chat = await chatRepository.GetActive(request.Id);
        if (chat is null) { return Json(new { success = false }); }

        if (chat.CompanyId != user.Id && chat.CustomerId != user.Id)
        {
            return Json(new { success = false });
        }

        // automated farewell message
        var message = await chatRepository.CreateMessage(new ChatMessageEntity
        {
            ChatId = chat.Id,
            SenderId = user.Id,
            Content = "Sorry, I kindly have to end our negotiation, because we couldn't reach an agreement. Wish you a good luck.",
            SentDate = DateTime.UtcNow,
            IsRead = false
        });

        // cancel chat and revert request to pending
        await chatRepository.Cancel(chat.Id);
        request.Status = RequestStatusEnum.Pending;
        request.ExecutorId = null;
        await requestRepository.Update(request);

        var group = "chat-" + chat.Key;

        // broadcast the automated message then signal cancellation to both parties
        await hubContext.Clients.Group(group).SendAsync("ReceiveMessage", new
        {
            id = message.Id,
            senderId = message.SenderId,
            content = message.Content,
            sentDate = message.SentDate.ToString("HH:mm"),
            sentDay = message.SentDate.ToString("yyyy-MM-dd")
        });

        await hubContext.Clients.Group(group).SendAsync("ChatCancelled");

        return Json(new { success = true });
    }

}
