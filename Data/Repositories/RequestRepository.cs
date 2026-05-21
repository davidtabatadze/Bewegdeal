using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class RequestRepository(SqlContext context) : IRequestRepository
    {

        // ── Write ────────────────────────────────────────────────────────────────

        public async Task<RequestEntity> Create(RequestEntity request)
        {
            context.Requests.Add(request);
            await context.SaveChangesAsync();
            return request;
        }

        public async Task Update(RequestEntity request)
        {
            context.Requests.Update(request);
            await context.SaveChangesAsync();
        }

        // ── Read ─────────────────────────────────────────────────────────────────

        public async Task<RequestEntity?> Get(long id) =>
            await context.Requests.FindAsync(id);

        public async Task<RequestEntity?> Get(string number) =>
            await context.Requests.FirstOrDefaultAsync(r => r.Number == number);

        public async Task<int> Count(RequestFilter filter) =>
            await ApplyFilters(context.Requests.AsQueryable(), filter).CountAsync();

        public async Task<List<RequestEntity>> Load(RequestFilter filter)
        {
            var query = ApplyFilters(context.Requests.AsQueryable(), filter);

            if (!string.IsNullOrWhiteSpace(filter.SortDirection) && !string.IsNullOrWhiteSpace(filter.SortField))
            {
                var desc = filter.SortDirection == SortDirectionEnum.Desc;
                query = filter.SortField switch
                {
                    SortFieldEnum.Status => desc ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
                    SortFieldEnum.Service => desc ? query.OrderByDescending(r => r.Service) : query.OrderBy(r => r.Service),
                    SortFieldEnum.CreateDate => desc ? query.OrderByDescending(r => r.CreateDate) : query.OrderBy(r => r.CreateDate),
                    _ => desc ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id)
                };
            }

            if (filter.Start.HasValue && filter.Length.HasValue)
            {
                query = query.Skip(filter.Start.Value).Take(filter.Length.Value);
            }

            return await query.ToListAsync();
        }

        private static IQueryable<RequestEntity> ApplyFilters(IQueryable<RequestEntity> query, RequestFilter filter)
        {
            if (filter.Id.HasValue)
            {
                query = query.Where(r => r.Id == filter.Id.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(r => r.Status == filter.Status);
            }

            if (!string.IsNullOrWhiteSpace(filter.Service))
            {
                query = query.Where(r => r.Service == filter.Service);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(r =>
                    r.Title.ToLower().Contains(term) ||
                    r.Number.ToLower().Contains(term)
                );
            }

            if (!string.IsNullOrWhiteSpace(filter.ViewerRole) && filter.ViewerId.HasValue)
            {
                var viewerId = filter.ViewerId.Value;
                if (filter.ViewerRole == UserRoleEnum.Administrator)
                {
                    // ...
                }
                else if (filter.ViewerRole == UserRoleEnum.Customer)
                {
                    query = query.Where(r => r.RequesterId == viewerId);
                }
                else if (filter.ViewerRole == UserRoleEnum.Company)
                {
                    query = query.Where(r =>
                        r.ExecutorId == viewerId ||
                        r.Status == RequestStatusEnum.Pending ||
                        r.Status == RequestStatusEnum.Negotiation
                    );
                }
                else
                {
                    query = query.Where(r => r.Id == 0);
                }
            }

            if (filter.ExecutorId.HasValue)
            {
                query = query.Where(r => r.ExecutorId == filter.ExecutorId.Value);
            }

            if (filter.DateFrom.HasValue)
            {
                query = query.Where(r => r.CreateDate >= filter.DateFrom.Value);
            }

            if (filter.DateTo.HasValue)
            {
                query = query.Where(r => r.CreateDate <= filter.DateTo.Value);
            }

            return query;
        }

        // ── Delete ───────────────────────────────────────────────────────────────
        // ***

    }
}
