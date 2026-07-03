using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class RequestProposalRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IRequestProposalRepository
    {

        public async Task Update(RequestProposalUpdateAreaEnum area, RequestProposalEntity update)
        {
            switch (area)
            {

                case RequestProposalUpdateAreaEnum.Status:
                    await Context.RequestProposals.Where(p => p.Id == update.Id)
                                                  .ExecuteUpdateAsync(p => p
                                                       .SetProperty(p => p.Status, update.Status)
                                                       .SetProperty(p => p.ReactionDate, DateTime.Now)
                                                       .SetProperty(p => p.ReactionReason, update.ReactionReason)
                                                       .SetProperty(p => p.InvoiceId, 0)
                                                  );
                    break;

                case RequestProposalUpdateAreaEnum.Invoice:
                    await Context.RequestProposals.Where(p => p.Id == update.Id)
                                                  .ExecuteUpdateAsync(p => p
                                                       .SetProperty(p => p.InvoiceId, update.InvoiceId)
                                                  );
                    break;

                default:
                    throw new ArgumentException("Invalid update area", nameof(area));
            }
        }

        public async Task<List<RequestProposalEntity>> Load(RequestProposalFilter filter)
            => await ApplyFilters(Context.RequestProposals.AsQueryable(), filter).ToListAsync();

        public async Task<int> Count(RequestProposalFilter filter)
            => await ApplyFilters(Context.RequestProposals.AsQueryable(), filter).CountAsync();

        private IQueryable<RequestProposalEntity> ApplyFilters(IQueryable<RequestProposalEntity> query, RequestProposalFilter filter)
        {
            if (filter.ChatId.HasValue)
            {
                query = query.Where(i => i.ChatId == filter.ChatId);
            }

            if (filter.InvoiceId.HasValue)
            {
                query = query.Where(i => i.InvoiceId == filter.InvoiceId);
            }

            if (filter.CompanyId.HasValue)
            {
                query = query.Where(i => i.CompanyId == filter.CompanyId);
            }

            if (filter.DateFrom.HasValue)
            {
                query = query.Where(i => i.Date >= filter.DateFrom);
            }

            if (filter.DateTo.HasValue)
            {
                query = query.Where(i => i.Date <= filter.DateTo);
            }

            if (filter.RequestIds is not null)
            {
                filter.RequestIds.Add(0);
                query = query.Where(i => filter.RequestIds.Contains(i.RequestId));
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(i => i.Status == filter.Status);
            }

            query = ApplySorting(query, filter);
            query = ApplyPaging(query, filter);

            return query;
        }

    }
}
