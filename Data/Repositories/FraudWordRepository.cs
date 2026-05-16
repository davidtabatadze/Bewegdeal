using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class FraudWordRepository(SqlContext context) : IFraudWordRepository
    {
        private readonly SqlContext _context = context;

        public async Task<FraudWordEntity?> Get(FraudWordFilter filter)
        {
            var query = _context.FraudWords.AsQueryable();

            if (filter.Id.HasValue)
            {
                query = query.Where(w => w.Id == filter.Id.Value);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<int> Count(FraudWordFilter filter)
        {
            return await ApplyFilters(_context.FraudWords.AsQueryable(), filter).CountAsync();
        }

        public async Task<List<FraudWordEntity>> Load(FraudWordFilter filter)
        {
            var query = ApplyFilters(_context.FraudWords.AsQueryable(), filter);

            query = query.OrderByDescending(w => w.CreatedAt);

            if (filter.Start.HasValue && filter.Length.HasValue)
            {
                query = query.Skip(filter.Start.Value).Take(filter.Length.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<FraudWordEntity> Create(FraudWordEntity entity)
        {
            _context.FraudWords.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task Update(long id, string word, string description)
        {
            await _context.FraudWords
                .Where(w => w.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(w => w.Word, word)
                    .SetProperty(w => w.Description, description)
                );
        }

        public async Task SetStatus(long id, string status)
        {
            await _context.FraudWords
                .Where(w => w.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(w => w.Status, status));
        }

        public async Task Delete(long id)
        {
            await _context.FraudWords
                .Where(w => w.Id == id)
                .ExecuteDeleteAsync();
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
