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
                                            c.SetProperty(p => p.Status, update.Status)
                                       );
                    break;

                case ChatUpdateAreaEnum.Fraud:
                    await Context.Chats.Where(c => c.Id == update.Id)
                                       .ExecuteUpdateAsync(c =>
                                            c.SetProperty(p => p.Fraud, update.Fraud)
                                       );
                    break;

                default:
                    throw new ArgumentException("Invalid update area", nameof(area));
            }
        }

        public async Task<ChatEntity?> Get(ChatFilter filter, string[]? properties = null)
            => await ApplyFilters(Context.Chats.AsQueryable(), filter).Select(BuildSelect<ChatEntity>(properties)).FirstOrDefaultAsync();

        public async Task<List<ChatEntity>> Load(ChatFilter filter)
            => await ApplyFilters(Context.Chats.AsQueryable(), filter).ToListAsync();

        public async Task<int> Count(ChatFilter filter)
            => await ApplyFilters(Context.Chats.AsQueryable(), filter).CountAsync();

        public async Task<ChatMessageEntity> AddMessage(ChatMessageEntity message)
            => await Create(message);

        public async Task ReadMessages(long chatId, long viewerId)
            => await Context.ChatMessages
                            .Where(m => m.ChatId == chatId && m.SenderId != viewerId && !m.IsRead)
                            .ExecuteUpdateAsync(u => u
                                .SetProperty(p => p.IsRead, true)
                            );

        public async Task<ChatMessageEntity?> GetMessageUnread(long userId)
        {
            var chats = await Context.Chats
                                     .Where(c => c.CustomerId == userId || c.CompanyId == userId)
                                     .Select(BuildSelect<ChatEntity>([nameof(ChatEntity.Id)]))
                                     .ToListAsync();
            var chatIds = chats.Select(c => c.Id).ToList().Concat([0]);

            return await Context.ChatMessages
                                .Where(m => chatIds.Contains(m.ChatId) && m.SenderId != userId && !m.IsRead)
                                .OrderByDescending(m => m.Id)
                                .FirstOrDefaultAsync();
        }

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

            if (filter.CompanyId.HasValue)
            {
                query = query.Where(r => r.CompanyId == filter.CompanyId.Value);
            }

            if (filter.RequestId.HasValue)
            {
                query = query.Where(r => r.RequestId == filter.RequestId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.RequestNumber))
            {
                query = query.Where(r => r.RequestNumber == filter.RequestNumber);
            }

            if (!string.IsNullOrWhiteSpace(filter.Key))
            {
                query = query.Where(r => r.Key == filter.Key);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(r => r.Status == filter.Status);
            }

            if (!string.IsNullOrWhiteSpace(filter.Fraud))
            {
                query = query.Where(r => r.Fraud == filter.Fraud);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                query = query.Where(r => r.Id == 0);
            }

            query = ApplySorting(query, filter);
            query = ApplyPaging(query, filter);
            return query;
        }

    }
}
