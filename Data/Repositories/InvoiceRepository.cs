using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class InvoiceRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IInvoiceRepository
    {
        public async Task Update(InvoiceUpdateAreaEnum area, InvoiceEntity update)
        {
            switch (area)
            {

                case InvoiceUpdateAreaEnum.Paid:
                    await Context.Invoices.Where(r => r.Id == update.Id && r.Status == InvoiceStatusEnum.Pending)
                                          .ExecuteUpdateAsync(r => r
                                               .SetProperty(p => p.Status, InvoiceStatusEnum.Paid)
                                               .SetProperty(p => p.PaymentDate, DateTime.Now)
                                          );
                    break;

                default:
                    throw new ArgumentException("Invalid update area", nameof(area));
            }
        }

        public async Task<InvoiceEntity?> Get(InvoiceFilter filter, string[]? properties = null)
            => await ApplyFilters(Context.Invoices.AsQueryable(), filter).Select(BuildSelect<InvoiceEntity>(properties)).FirstOrDefaultAsync();

        public async Task<List<InvoiceEntity>> Load(InvoiceFilter filter)
            => await ApplyFilters(Context.Invoices.AsQueryable(), filter).ToListAsync();

        public async Task<int> Count(InvoiceFilter filter)
            => await ApplyFilters(Context.Invoices.AsQueryable(), filter).CountAsync();

        private IQueryable<InvoiceEntity> ApplyFilters(IQueryable<InvoiceEntity> query, InvoiceFilter filter)
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

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(r =>
                    r.Number.ToLower().Contains(term) ||
                    r.Status.ToLower().Contains(term) ||
                    r.RequestId.ToString().ToLower().Contains(term)
                );
            }

            query = ApplySorting(query, filter);
            query = ApplyPaging(query, filter);

            return query;
        }
    }
}
