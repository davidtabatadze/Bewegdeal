using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Bewegdeal.Hubs
{
    public class ChatHub(
        IChatRepository chatRepository,
        IUserRepository userRepository,
        IRequestRepository requestRepository) : Hub
    {
        /// <summary>
        /// Joins the caller's personal notification group so they receive new-message alerts on any page.
        /// Also replays one notification per active chat that has unread messages, so alerts are not
        /// missed when the user was offline when the messages arrived.
        /// </summary>
        public async Task JoinNotifications()
        {
            var userId = GetUserId();
            if (userId == 0) { return; }

            await Groups.AddToGroupAsync(Context.ConnectionId, "user-" + userId);

            // Catchup: fire one notification per chat with unread messages
            var unread = await chatRepository.LoadUnreadForUser(userId);
            foreach (var summary in unread)
            {
                await Clients.Caller.SendAsync("NewMessageNotification", new
                {
                    senderName = summary.SenderName,
                    preview = summary.Preview,
                    requestNumber = summary.RequestNumber
                });
            }
        }

        /// <summary>
        /// Joins the SignalR group for a specific chat, after verifying the caller is a participant.
        /// </summary>
        public async Task JoinChat(string chatKey)
        {
            var userId = GetUserId();
            if (userId == 0) { return; }

            var chat = await chatRepository.Get(chatKey);
            if (chat is null || !IsParticipant(chat, userId)) { return; }

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(chatKey));

            // mark incoming messages as read on join, then notify the sender
            await chatRepository.MarkRead(chat.Id, userId);
            await Clients.OthersInGroup(GroupName(chatKey)).SendAsync("MessagesRead");
        }

        /// <summary>
        /// Saves the message to the database and broadcasts it to all group members.
        /// </summary>
        public async Task SendMessage(string chatKey, string content)
        {
            var userId = GetUserId();
            if (userId == 0) { return; }

            content = (content ?? "").Trim();
            if (string.IsNullOrWhiteSpace(content) || content.Length > 2048) { return; }

            var chat = await chatRepository.Get(chatKey);
            if (chat is null || !IsParticipant(chat, userId) || chat.Status != ChatStatusEnum.Active)
            {
                return;
            }

            var message = await chatRepository.CreateMessage(new ChatMessageEntity
            {
                ChatId = chat.Id,
                SenderId = userId,
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
            var recipientId = userId == chat.CompanyId ? chat.CustomerId : chat.CompanyId;
            var sender = await userRepository.Get(new UserFilter { Id = userId });
            var request = await requestRepository.Get(chat.RequestId);
            var preview = content.Length > 80 ? content[..80] + "…" : content;

            await Clients.Group("user-" + recipientId).SendAsync("NewMessageNotification", new
            {
                senderName = sender?.Name ?? "Someone",
                preview = preview,
                requestNumber = request?.Number ?? ""
            });
        }

        /// <summary>
        /// Marks all messages from the other party as read and notifies them.
        /// Called by the receiver when a new message arrives while they are already in the chat.
        /// </summary>
        public async Task MarkRead(string chatKey)
        {
            var userId = GetUserId();
            if (userId == 0) { return; }

            var chat = await chatRepository.Get(chatKey);
            if (chat is null || !IsParticipant(chat, userId)) { return; }

            await chatRepository.MarkRead(chat.Id, userId);
            await Clients.OthersInGroup(GroupName(chatKey)).SendAsync("MessagesRead");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private long GetUserId()
        {
            var session = Context.GetHttpContext()?.Session;
            return long.TryParse(session?.GetString(ConstantEnum.SessionUserId), out var id) ? id : 0;
        }

        private static bool IsParticipant(ChatEntity chat, long userId) =>
            chat.CustomerId == userId || chat.CompanyId == userId;

        private static string GroupName(string chatKey) => "chat-" + chatKey;
    }
}
