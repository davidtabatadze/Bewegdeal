using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class ChatRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IChatRepository
    {

        public async Task Update(ChatUpdateAreaEnum area, ChatEntity update)
        {
            switch (area)
            {

                case ChatUpdateAreaEnum.Status:
                    await Context.Chats.Where(c => c.Id == update.Id)
                                       .ExecuteUpdateAsync(c =>
                                            c.SetProperty(p => p.Status, ChatStatusEnum.Cancelled)
                                       );
                    break;

                default:
                    throw new ArgumentException("Invalid update area", nameof(area));
            }
        }

        public async Task<ChatEntity?> Get(ChatFilter filter, string[]? properties = null)
            => await ApplyFilters(Context.Chats.AsQueryable(), filter).Select(BuildSelect<ChatEntity>(properties)).FirstOrDefaultAsync();

        public async Task<ChatMessageEntity> AddMessage(ChatMessageEntity message)
            => await Create(message);

        public async Task ReadMessages(long chatId, long viewerId)
            => await Context.ChatMessages
                            .Where(m => m.ChatId == chatId && m.SenderId != viewerId && !m.IsRead)
                            .ExecuteUpdateAsync(u => u
                                .SetProperty(p => p.IsRead, true)
                            );

        public async Task<List<ChatMessageEntity>> LoadMessages(long chatId)
            => await Context.ChatMessages
                            .Where(m => m.ChatId == chatId)
                            .OrderBy(m => m.SentDate)
                            .ToListAsync();

        private IQueryable<ChatEntity> ApplyFilters(IQueryable<ChatEntity> query, ChatFilter filter)
        {
            if (filter.Id.HasValue)
            {
                query = query.Where(r => r.Id == filter.Id.Value);
            }

            if (filter.RequestId.HasValue)
            {
                query = query.Where(r => r.RequestId == filter.RequestId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Key))
            {
                query = query.Where(r => r.Key == filter.Key);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(r => r.Status == filter.Status);
            }

            query = ApplySorting(query, filter);
            query = ApplyPaging(query, filter);

            return query;
        }



        //public async Task<ChatEntity?> Get(string key) =>
        //    await Context.Chats.FirstOrDefaultAsync(c => c.Key == key);

        //public async Task<ChatEntity?> GetActive(long requestId) =>
        //    await Context.Chats.FirstOrDefaultAsync(c =>
        //        c.RequestId == requestId &&
        //        c.Status == ChatStatusEnum.Active);

        // ── Messages ─────────────────────────────────────────────────────────────

        //public async Task<ChatMessageEntity> CreateMessage(ChatMessageEntity message)
        //{
        //    Context.ChatMessages.Add(message);
        //    await Context.SaveChangesAsync();
        //    return message;
        //}



        //public async Task MarkRead(long chatId, long viewerId)
        //{
        //    var unread = await Context.ChatMessages
        //        .Where(m => m.ChatId == chatId && m.SenderId != viewerId && !m.IsRead)
        //        .ToListAsync();

        //    foreach (var m in unread)
        //    {
        //        m.IsRead = true;
        //    }

        //    if (unread.Count > 0)
        //    {
        //        await Context.SaveChangesAsync();
        //    }
        //}

        //public async Task Cancel(long chatId)
        //{
        //    var chat = await Context.Chats.FindAsync(chatId);
        //    if (chat is null) { return; }
        //    chat.Status = ChatStatusEnum.Cancelled;
        //    await Context.SaveChangesAsync();
        //}


















        //public async Task<List<ChatUnreadSummary>> LoadUnreadForUser(long userId)
        //{
        //    // Find active chats where the user is a participant and has at least one unread message
        //    var chats = await Context.Chats
        //        .Where(c => c.Status == ChatStatusEnum.Active &&
        //                    (c.CustomerId == userId || c.CompanyId == userId))
        //        .ToListAsync();

        //    var results = new List<ChatUnreadSummary>();

        //    foreach (var chat in chats)
        //    {
        //        // Latest unread message sent by the OTHER party
        //        var latest = await Context.ChatMessages
        //            .Where(m => m.ChatId == chat.Id && m.SenderId != userId && !m.IsRead)
        //            .OrderByDescending(m => m.SentDate)
        //            .FirstOrDefaultAsync();

        //        if (latest is null) { continue; }

        //        var senderId = latest.SenderId;
        //        var sender = await Context.Users.FindAsync(senderId);
        //        var request = await Context.Requests.FindAsync(chat.RequestId);

        //        var preview = latest.Content.Length > 80
        //            ? latest.Content[..80] + "…"
        //            : latest.Content;

        //        results.Add(new ChatUnreadSummary
        //        {
        //            SenderName = sender?.Name ?? "Someone",
        //            Preview = preview,
        //            RequestNumber = request?.Number ?? ""
        //        });
        //    }

        //    return results;
        //}
    }
}
