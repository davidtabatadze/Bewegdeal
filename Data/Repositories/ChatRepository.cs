using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class ChatRepository(SqlContext context) : IChatRepository
    {
        // ── Chat ─────────────────────────────────────────────────────────────────

        public async Task<ChatEntity> Create(ChatEntity chat)
        {
            context.Chats.Add(chat);
            await context.SaveChangesAsync();
            return chat;
        }

        public async Task<ChatEntity?> Get(long id) =>
            await context.Chats.FindAsync(id);

        public async Task<ChatEntity?> Get(string key) =>
            await context.Chats.FirstOrDefaultAsync(c => c.Key == key);

        public async Task<ChatEntity?> GetActive(long requestId) =>
            await context.Chats.FirstOrDefaultAsync(c =>
                c.RequestId == requestId &&
                c.Status == ChatStatusEnum.Active);

        // ── Messages ─────────────────────────────────────────────────────────────

        public async Task<ChatMessageEntity> CreateMessage(ChatMessageEntity message)
        {
            context.ChatMessages.Add(message);
            await context.SaveChangesAsync();
            return message;
        }

        public async Task<List<ChatMessageEntity>> LoadMessages(long chatId) =>
            await context.ChatMessages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.SentDate)
                .ToListAsync();

        public async Task MarkRead(long chatId, long viewerId)
        {
            var unread = await context.ChatMessages
                .Where(m => m.ChatId == chatId && m.SenderId != viewerId && !m.IsRead)
                .ToListAsync();

            foreach (var m in unread)
            {
                m.IsRead = true;
            }

            if (unread.Count > 0)
            {
                await context.SaveChangesAsync();
            }
        }

        public async Task Cancel(long chatId)
        {
            var chat = await context.Chats.FindAsync(chatId);
            if (chat is null) { return; }
            chat.Status = ChatStatusEnum.Cancelled;
            await context.SaveChangesAsync();
        }
    }
}
