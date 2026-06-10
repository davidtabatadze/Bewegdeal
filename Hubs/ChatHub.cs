using Bewegdeal.Data.Entities;
using Bewegdeal.Enums;
using Bewegdeal.Services;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Bewegdeal.Hubs
{
    public class ChatHub(ChatService ChatService) : Hub
    {

        public async Task JoinNotifications()
        {
            return;

            //if (UserId == 0) { return; }

            //await Groups.AddToGroupAsync(Context.ConnectionId, "user-" + UserId);

            //// Catchup: fire one notification per chat with unread messages
            //var unread = await chatRepository.LoadUnreadForUser(UserId);
            //foreach (var summary in unread)
            //{
            //    await Clients.Caller.SendAsync("NewMessageNotification", new
            //    {
            //        senderName = summary.SenderName,
            //        preview = summary.Preview,
            //        requestNumber = summary.RequestNumber
            //    });
            //}
        }

        public async Task JoinChat(string chatKey)
        {
            var chat = await ChatService.Get(chatKey);
            if (chat is null || !IsParticipant(chat, UserId))
            {
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(chatKey));
            await MarkRead(chatKey, chat);
        }

        public async Task SendMessage(string chatKey, string content)
        {
            content = (content ?? "").Trim();
            if (string.IsNullOrWhiteSpace(content) || content.Length > 2048) // why?
            {
                return;
            }

            var chat = await ChatService.Get(chatKey);
            if (chat is null || !IsParticipant(chat, UserId) || chat.Status != ChatStatusEnum.Active)
            {
                return;
            }

            var message = await ChatService.AddMessage(new ChatMessageEntity
            {
                ChatId = chat.Id,
                SenderId = UserId,
                Content = content,
                SentDate = DateTime.UtcNow,
                IsRead = false
            });

            await Clients.Group(GroupName(chatKey)).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderId = message.SenderId,
                content = message.Content,
                sentDate = message.SentDate.ToString("HH:mm"),
                sentDay = message.SentDate.ToString("yyyy-MM-dd")
            });

            // notify the recipient's personal group (for other-page toast / browser notification)
            //var recipientId = UserId == chat.CompanyId ? chat.CustomerId : chat.CompanyId;
            //var sender = await userService.Get(UserId);
            //var request = await requestRepository.Get<RequestEntity>(chat.RequestId);
            //var preview = content.Length > 80 ? content[..80] + "…" : content;

            //await Clients.Group("user-" + recipientId).SendAsync("NewMessageNotification", new
            //{
            //    senderName = sender?.Name ?? "Someone",
            //    preview = preview,
            //    requestNumber = request?.Number ?? ""
            //});
        }

        public async Task MarkRead(string chatKey, ChatEntity? chat = null)
        {
            chat ??= await ChatService.Get(chatKey);
            if (chat is null || !IsParticipant(chat, UserId))
            {
                return;
            }

            await ChatService.ReadMessages(chat.Id, UserId);
            await Clients.OthersInGroup(GroupName(chatKey)).SendAsync("MessagesRead");
        }

        public async Task LeaveChat(string chatKey)
        {
            //var chat = await ChatService.Get(chatKey);
            //if (chat is null || !IsParticipant(chat, UserId)) { return; }
            //await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(chatKey));
            await Clients.Group(GroupName(chatKey)).SendAsync("ChatCancelled");
        }

        private static string GroupName(string chatKey) => "bewegdeal-chat-" + chatKey;
        private long UserId => long.TryParse(Context.User?.FindFirstValue(IdentityFieldEnum.Id), out var id) ? id : 0;
        private static bool IsParticipant(ChatEntity chat, long userId) => chat.CustomerId == userId || chat.CompanyId == userId;

    }
}
