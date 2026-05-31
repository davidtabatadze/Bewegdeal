using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class ChatRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IChatRepository
    {
        // ── Chat ─────────────────────────────────────────────────────────────────


        public async Task<ChatEntity?> Get(string key) =>
            await Context.Chats.FirstOrDefaultAsync(c => c.Key == key);

        public async Task<ChatEntity?> GetActive(long requestId) =>
            await Context.Chats.FirstOrDefaultAsync(c =>
                c.RequestId == requestId &&
                c.Status == ChatStatusEnum.Active);

        // ── Messages ─────────────────────────────────────────────────────────────

        public async Task<ChatMessageEntity> CreateMessage(ChatMessageEntity message)
        {
            Context.ChatMessages.Add(message);
            await Context.SaveChangesAsync();
            return message;
        }

        public async Task<List<ChatMessageEntity>> LoadMessages(long chatId) =>
            await Context.ChatMessages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.SentDate)
                .ToListAsync();

        public async Task MarkRead(long chatId, long viewerId)
        {
            var unread = await Context.ChatMessages
                .Where(m => m.ChatId == chatId && m.SenderId != viewerId && !m.IsRead)
                .ToListAsync();

            foreach (var m in unread)
            {
                m.IsRead = true;
            }

            if (unread.Count > 0)
            {
                await Context.SaveChangesAsync();
            }
        }

        public async Task Cancel(long chatId)
        {
            var chat = await Context.Chats.FindAsync(chatId);
            if (chat is null) { return; }
            chat.Status = ChatStatusEnum.Cancelled;
            await Context.SaveChangesAsync();
        }

        /// <summary>
        /// For each active chat where the user is a participant, returns one summary per chat
        /// if there are unread messages from the other party — using the latest such message as preview.
        /// </summary>
        public async Task<List<ChatUnreadSummary>> LoadUnreadForUser(long userId)
        {
            // Find active chats where the user is a participant and has at least one unread message
            var chats = await Context.Chats
                .Where(c => c.Status == ChatStatusEnum.Active &&
                            (c.CustomerId == userId || c.CompanyId == userId))
                .ToListAsync();

            var results = new List<ChatUnreadSummary>();

            foreach (var chat in chats)
            {
                // Latest unread message sent by the OTHER party
                var latest = await Context.ChatMessages
                    .Where(m => m.ChatId == chat.Id && m.SenderId != userId && !m.IsRead)
                    .OrderByDescending(m => m.SentDate)
                    .FirstOrDefaultAsync();

                if (latest is null) { continue; }

                var senderId = latest.SenderId;
                var sender = await Context.Users.FindAsync(senderId);
                var request = await Context.Requests.FindAsync(chat.RequestId);

                var preview = latest.Content.Length > 80
                    ? latest.Content[..80] + "…"
                    : latest.Content;

                results.Add(new ChatUnreadSummary
                {
                    SenderName = sender?.Name ?? "Someone",
                    Preview = preview,
                    RequestNumber = request?.Number ?? ""
                });
            }

            return results;
        }
    }
}
