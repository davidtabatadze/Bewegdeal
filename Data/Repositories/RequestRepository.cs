using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class RequestRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IRequestRepository
    {

        public async Task Update(RequestUpdateAreaEnum area, RequestEntity update)
        {
            switch (area)
            {

                case RequestUpdateAreaEnum.Full:
                    await Update(update);
                    break;

                case RequestUpdateAreaEnum.Status:
                    await Context.Requests.Where(r => r.Id == update.Id)
                                          .ExecuteUpdateAsync(r => r
                                               .SetProperty(p => p.Status, update.Status)
                                          );
                    break;

                case RequestUpdateAreaEnum.ChatActivate:
                    await Context.Requests.Where(r => r.Id == update.Id && r.Status == RequestStatusEnum.Pending)
                                          .ExecuteUpdateAsync(r => r
                                               .SetProperty(p => p.Status, RequestStatusEnum.Negotiation)
                                               .SetProperty(p => p.ExecutorId, (long?)null)
                                               .SetProperty(p => p.AgreementId, (long?)null)
                                          );
                    break;

                case RequestUpdateAreaEnum.ChatDeal:
                    await Context.Requests.Where(r => r.Id == update.Id && r.Status == RequestStatusEnum.Negotiation)
                                          .ExecuteUpdateAsync(r => r
                                               .SetProperty(p => p.Status, RequestStatusEnum.Agreed)
                                               .SetProperty(p => p.ExecutorId, update.ExecutorId)
                                               .SetProperty(p => p.AgreementId, update.AgreementId)
                                          );
                    break;

                case RequestUpdateAreaEnum.ChatDeactivate:
                    await Context.Requests.Where(r => r.Id == update.Id && r.Status == RequestStatusEnum.Negotiation)
                                          .ExecuteUpdateAsync(r => r
                                               .SetProperty(p => p.Status, RequestStatusEnum.Pending)
                                               .SetProperty(p => p.ExecutorId, (long?)null)
                                               .SetProperty(p => p.AgreementId, (long?)null)
                                          );
                    break;

                default:
                    throw new ArgumentException("Invalid update area", nameof(area));
            }
        }

        public async Task<RequestEntity?> Get(RequestFilter filter, string[]? properties = null)
            => await ApplyFilters(Context.Requests.AsQueryable(), filter).Select(BuildSelect<RequestEntity>(properties)).FirstOrDefaultAsync();

        public async Task<List<RequestEntity>> Load(RequestFilter filter)
            => await ApplyFilters(Context.Requests.AsQueryable(), filter).ToListAsync();

        public async Task<int> Count(RequestFilter filter)
            => await ApplyFilters(Context.Requests.AsQueryable(), filter).CountAsync();

        public async Task<int> CountDistinct(RequestFilter filter, string property)
        {
            var query = ApplyFilters(Context.Requests.AsQueryable(), filter);
            return property switch
            {
                nameof(RequestEntity.Status) => await query.Select(r => r.Status).Distinct().CountAsync(),
                _ => throw new ArgumentException("Invalid distinct property", nameof(property))
            };
        }

        private IQueryable<RequestEntity> ApplyFilters(IQueryable<RequestEntity> query, RequestFilter filter)
        {
            if (filter.Id.HasValue)
            {
                query = query.Where(r => r.Id == filter.Id.Value);
            }

            if (filter.Ids is not null)
            {
                filter.Ids.Add(0);
                query = query.Where(r => filter.Ids.Contains(r.Id));
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
