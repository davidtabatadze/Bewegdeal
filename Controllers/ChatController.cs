using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers;

[RequireLogin]
public class ChatController(
    IUserRepository userRepository,
    IFileRepository fileRepository,
    IRequestRepository requestRepository,
    IChatRepository chatRepository) : XBaseController(userRepository)
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
    /// Full chat context — party names, pictures, messages.
    /// Called only when the offcanvas opens, while a spinner is visible.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Context(string requestNumber)
    {
        var user = await GetUser();
        if (user is null) { return Json(new { }); }

        var request = await requestRepository.Get(requestNumber);
        if (request is null) { return Json(new { }); }

        var activeChat = await chatRepository.GetActive(request.Id);

        // viewer info (for message bubble avatars)
        var (_, viewerInitials, viewerPictureUrl) = await ResolveParty(user);

        // other party: for "initiate" it's the requester; for "active" it's whoever isn't the viewer
        var isActive = activeChat is not null;
        long otherPartyId = isActive
            ? (user.Role == UserRoleEnum.Customer ? activeChat!.CompanyId : activeChat!.CustomerId)
            : request.RequesterId;

        var otherParty = await _userRepository.Get(new UserFilter { Id = otherPartyId });
        var (otherPartyName, otherPartyInitials, otherPartyPictureUrl) = await ResolveParty(otherParty);

        var messages = isActive
            ? (await chatRepository.LoadMessages(activeChat!.Id))
                .Select(m => new
                {
                    senderId = m.SenderId,
                    content = m.Content,
                    sentDate = m.SentDate.ToString("HH:mm")
                })
                .ToList<object>()
            : new List<object>();

        return Json(new
        {
            chatKey = activeChat?.Key ?? "",
            viewerId = user.Id,
            viewerInitials,
            viewerPictureUrl,
            otherPartyName,
            otherPartyInitials,
            otherPartyPictureUrl,
            messages
        });
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
