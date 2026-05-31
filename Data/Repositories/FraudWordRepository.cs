using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class FraudWordRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IFraudWordRepository
    {

        public async Task<FraudWordEntity?> Get(FraudWordFilter filter)
        {
            var query = Context.FraudWords.AsQueryable();

            if (filter.Id.HasValue)
            {
                query = query.Where(w => w.Id == filter.Id.Value);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<int> Count(FraudWordFilter filter)
        {
            return await ApplyFilters(Context.FraudWords.AsQueryable(), filter).CountAsync();
        }

        public async Task<List<FraudWordEntity>> Load(FraudWordFilter filter)
        {
            var query = ApplyFilters(Context.FraudWords.AsQueryable(), filter);

            query = query.OrderByDescending(w => w.CreatedAt);

            if (filter.Start.HasValue && filter.Length.HasValue)
            {
                query = query.Skip(filter.Start.Value).Take(filter.Length.Value);
            }

            return await query.ToListAsync();
        }



        public async Task Update(long id, string word, string description)
        {
            await Context.FraudWords
                .Where(w => w.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(w => w.Word, word)
                    .SetProperty(w => w.Description, description)
                );
        }

        public async Task SetStatus(long id, string status)
        {
            await Context.FraudWords
                .Where(w => w.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(w => w.Status, status));
        }



        private static IQueryable<FraudWordEntity> ApplyFilters(IQueryable<FraudWordEntity> query, FraudWordFilter filter)
        {
            if (filter.Id.HasValue)
            {
                query = query.Where(w => w.Id == filter.Id.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(w => w.Status == filter.Status);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(w =>
                    w.Word.ToLower().Contains(term) ||
                    w.Description.ToLower().Contains(term)
                );
            }

            return query;
        }
    }
}
