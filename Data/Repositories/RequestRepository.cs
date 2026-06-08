using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class RequestRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IRequestRepository
    {

        public async Task<RequestEntity?> Get(RequestFilter filter, string[]? properties = null)
            => await ApplyFilters(Context.Requests.AsQueryable(), filter).Select(BuildSelect<RequestEntity>(properties)).FirstOrDefaultAsync();

        public async Task<List<RequestEntity>> Load(RequestFilter filter)
            => await ApplyFilters(Context.Requests.AsQueryable(), filter).ToListAsync();

        public async Task<int> Count(RequestFilter filter)
            => await ApplyFilters(Context.Requests.AsQueryable(), filter).CountAsync();

        private IQueryable<RequestEntity> ApplyFilters(IQueryable<RequestEntity> query, RequestFilter filter)
        {
            if (filter.Id.HasValue)
            {
                query = query.Where(r => r.Id == filter.Id.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Number))
            {
                query = query.Where(r => r.Number == filter.Number);
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
                    r.Number.ToLower().Contains(term) ||
                    r.Status.ToLower().Contains(term) ||
                    r.Service.ToLower().Contains(term)
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
                    var interests = filter.ViewerInterests;
                    var hasMoving = interests.Contains(ServiceEnum.Moving);
                    var hasPickup = interests.Contains(ServiceEnum.Pickup);
                    var hasRemoval = interests.Contains(ServiceEnum.Removal);
                    var hasTransport = interests.Contains(ServiceEnum.Transport);

                    if (filter.ViewerFocus == RequestViewerFocusEnum.Mine)
                    {
                        query = query.Where(r => r.ExecutorId == viewerId);
                    }
                    else if (filter.ViewerFocus == RequestViewerFocusEnum.Potential)
                    {
                        query = query.Where(r =>
                            r.ExecutorId != viewerId &&
                            (r.Status == RequestStatusEnum.Pending || r.Status == RequestStatusEnum.Negotiation) &&
                            (
                                (hasMoving && r.Service == ServiceEnum.Moving) ||
                                (hasPickup && r.Service == ServiceEnum.Pickup) ||
                                (hasRemoval && r.Service == ServiceEnum.Removal) ||
                                (hasTransport && r.Service == ServiceEnum.Transport)
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(r =>
                            r.ExecutorId == viewerId ||
                            (
                                (r.Status == RequestStatusEnum.Pending || r.Status == RequestStatusEnum.Negotiation) &&
                                (
                                    (hasMoving && r.Service == ServiceEnum.Moving) ||
                                    (hasPickup && r.Service == ServiceEnum.Pickup) ||
                                    (hasRemoval && r.Service == ServiceEnum.Removal) ||
                                    (hasTransport && r.Service == ServiceEnum.Transport)
                                )
                            )
                        );
                    }
                }
                else
                {
                    query = query.Where(r => r.Id == 0);
                }
            }

            query = ApplySorting(query, filter);
            query = ApplyPaging(query, filter);

            return query;
        }

    }
}
