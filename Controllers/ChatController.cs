using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Bewegdeal.Hubs;
using Bewegdeal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Bewegdeal.Controllers;

[RequireLogin]
public class ChatController(
    IUserRepository userRepository,
    IFileRepository fileRepository,
    IRequestRepository requestRepository,
    IChatRepository chatRepository,
    IHubContext<ChatHub> hubContext) : XBaseController(userRepository)
{
    private readonly IUserRepository _userRepository = userRepository;

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
        var customer = await _userRepository.Get(new UserFilter { Id = request.RequesterId });
        var (name, initials, pictureUrl) = await ResolveParty(customer);

        return Json(new
        {
            success = true,
            chatKey = chat.Key,
            otherPartyName = name,
            otherPartyInitials = initials,
            otherPartyPictureUrl = pictureUrl
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

        var (_, viewerInitials, viewerPictureUrl) = await ResolveParty(user);

        long otherPartyId = activeChat is not null
            ? (user.Role == UserRoleEnum.Customer ? activeChat.CompanyId : activeChat.CustomerId)
            : request.RequesterId;

        var otherParty = await _userRepository.Get(new UserFilter { Id = otherPartyId });
        var (otherPartyName, otherPartyInitials, otherPartyPictureUrl) = await ResolveParty(otherParty);

        var messages = activeChat is not null
            ? await chatRepository.LoadMessages(activeChat.Id)
            : [];

        return PartialView("Conversation", new ChatHistoryViewModel
        {
            Mode               = mode,
            ChatKey            = activeChat?.Key ?? "",
            ViewerId           = user.Id,
            ViewerInitials     = viewerInitials,
            ViewerPictureUrl   = viewerPictureUrl,
            OtherPartyName     = otherPartyName,
            OtherPartyInitials = otherPartyInitials,
            OtherPartyPictureUrl = otherPartyPictureUrl,
            Messages           = messages
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
            ChatId    = chat.Id,
            SenderId  = user.Id,
            Content   = "Sorry, I kindly have to end our negotiation, because we couldn't reach an agreement. Wish you a good luck.",
            SentDate  = DateTime.UtcNow,
            IsRead    = false
        });

        // cancel chat and revert request to pending
        await chatRepository.Cancel(chat.Id);
        request.Status     = RequestStatusEnum.Pending;
        request.ExecutorId = null;
        await requestRepository.Update(request);

        var group = "chat-" + chat.Key;

        // broadcast the automated message then signal cancellation to both parties
        await hubContext.Clients.Group(group).SendAsync("ReceiveMessage", new
        {
            id       = message.Id,
            senderId = message.SenderId,
            content  = message.Content,
            sentDate = message.SentDate.ToString("HH:mm"),
            sentDay  = message.SentDate.ToString("yyyy-MM-dd")
        });

        await hubContext.Clients.Group(group).SendAsync("ChatCancelled");

        return Json(new { success = true });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(string Name, string Initials, string? PictureUrl)> ResolveParty(UserEntity? user)
    {
        var name = user?.Name ?? "-";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initials = string.Concat(parts.Take(2).Select(p => char.ToUpper(p[0])));
        if (string.IsNullOrEmpty(initials)) { initials = "?"; }

        string? pictureUrl = null;
        if (user?.ProfilePictureFileId.HasValue == true)
        {
            var file = await fileRepository.Get(user.ProfilePictureFileId.Value);
            if (file is not null)
            {
                pictureUrl = Url.Action("Download", "File", new { key = file.Key });
            }
        }

        return (name, initials, pictureUrl);
    }
}
