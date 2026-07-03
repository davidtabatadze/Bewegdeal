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

                case InvoiceUpdateAreaEnum.Status:
                    await Context.Invoices.Where(r => r.Id == update.Id)
                                          .ExecuteUpdateAsync(r => r
                                               .SetProperty(p => p.Status, update.Status)
                                               .SetProperty(p => p.PaymentDate, update.PaymentDate)
                                          );
                    break;

                case InvoiceUpdateAreaEnum.Cancel:
                    await Context.Invoices.Where(r => r.Status == InvoiceStatusEnum.Pending && r.RequestId == update.RequestId)
                                          .ExecuteUpdateAsync(r => r
                                               .SetProperty(p => p.Status, InvoiceStatusEnum.Cancelled)
                                               .SetProperty(p => p.PaymentDate, (DateTime?)null)
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

        public async Task<decimal> Sum(InvoiceFilter filter, string property)
        {
            var query = ApplyFilters(Context.Invoices.AsQueryable(), filter);
            return property switch
            {
                nameof(InvoiceEntity.ServiceCost) => await query.SumAsync(s => s.ServiceCost),
                nameof(InvoiceEntity.TotalCost) => await query.SumAsync(s => s.TotalCost),
                _ => throw new ArgumentException("Invalid sum property", nameof(property))
            };
        }

        public async Task<int> CountDistinct(InvoiceFilter filter, string property)
        {
            var query = ApplyFilters(Context.Invoices.AsQueryable(), filter);
            return property switch
            {
                nameof(InvoiceEntity.CompanyId) => await query.Select(s => s.CompanyId).Distinct().CountAsync(),
                nameof(InvoiceEntity.CustomerId) => await query.Select(s => s.CustomerId).Distinct().CountAsync(),
                nameof(InvoiceEntity.RequestId) => await query.Select(s => s.RequestId).Distinct().CountAsync(),
                _ => throw new ArgumentException("Invalid distinct property", nameof(property))
            };
        }

        public async Task<int> Count(InvoiceFilter filter)
            => await ApplyFilters(Context.Invoices.AsQueryable(), filter).CountAsync();

        private IQueryable<InvoiceEntity> ApplyFilters(IQueryable<InvoiceEntity> query, InvoiceFilter filter)
        {
            if (filter.Id.HasValue)
            {
                query = query.Where(r => r.Id == filter.Id.Value);
            }

            if (filter.RequestId.HasValue)
            {
                query = query.Where(r => r.RequestId == filter.RequestId.Value);
            }

            if (filter.DateFrom.HasValue)
            {
                filter.DateFrom = filter.DateFrom.Value.Date;
                query = query.Where(r => r.CreateDate >= filter.DateFrom);
            }

            if (filter.DateTo.HasValue)
            {
                filter.DateTo = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.CreateDate <= filter.DateTo.Value);
            }

            if (filter.AmountFrom.HasValue)
            {
                query = query.Where(r =>
                   r.ServiceCost >= filter.AmountFrom.Value ||
                   r.TotalCost >= filter.AmountFrom.Value
               );
            }

            if (filter.AmountTo.HasValue)
            {
                query = query.Where(r =>
                   r.ServiceCost <= filter.AmountTo.Value ||
                   r.TotalCost <= filter.AmountTo.Value
               );
            }

            if (filter.Active.HasValue && filter.Active == true)
            {
                query = query.Where(r => r.Status != InvoiceStatusEnum.Cancelled);
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

            if (!string.IsNullOrWhiteSpace(filter.ViewerRole) && filter.ViewerId.HasValue)
            {
                var viewerId = filter.ViewerId.Value;
                if (filter.ViewerRole == UserRoleEnum.Administrator)
                {
                    // ...
                }
                else if (filter.ViewerRole == UserRoleEnum.Company)
                {
                    query = query.Where(r => r.CompanyId == viewerId);
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
