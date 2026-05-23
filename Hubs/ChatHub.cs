using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Bewegdeal.Hubs
{
    public class ChatHub(IChatRepository chatRepository) : Hub
    {
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

            // mark incoming messages as read on join
            await chatRepository.MarkRead(chat.Id, userId);
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
                sentDate = message.SentDate.ToString("HH:mm")
            });
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
