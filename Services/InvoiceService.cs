using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Models;

namespace Bewegdeal.Services
{
    public class InvoiceService(IInvoiceRepository InvoiceRepository, UserService UserService)
    {
        public async Task<InvoiceEntity> Create(InvoiceEntity invoice)
            => await InvoiceRepository.Create(invoice);
        public async Task Update(InvoiceUpdateAreaEnum area, InvoiceEntity update)
            => await InvoiceRepository.Update(area, update);
        public async Task<InvoiceEntity?> Get(long id, string[]? properties = null)
            => await InvoiceRepository.Get<InvoiceEntity>(id, properties);
        public async Task<InvoiceEntity?> Get(InvoiceFilter filter, string[]? properties = null)
            => await InvoiceRepository.Get(filter, properties);
        private async Task<decimal> Sum(InvoiceFilter filter, string property)
            => await InvoiceRepository.Sum(filter, property);
        private async Task<int> Count(InvoiceFilter filter)
            => await InvoiceRepository.Count(filter);
        private async Task<int> CountDistinct(InvoiceFilter filter, string property)
            => await InvoiceRepository.CountDistinct(filter, property);

        public async Task<GenericResultModel<dynamic>> LoadGrid(long userId, string userRole)
        {
            var filter = new InvoiceFilter { ViewerId = userId, ViewerRole = userRole };

            var total = await Count(filter);
            var distinct = userRole == UserRoleEnum.Administrator ?
                           nameof(InvoiceEntity.CompanyId) : nameof(InvoiceEntity.CustomerId);
            var users = await CountDistinct(filter, distinct);

            filter.Status = InvoiceStatusEnum.Paid;
            var paid = await Sum(filter, nameof(InvoiceEntity.TotalCost));

            filter.Status = InvoiceStatusEnum.Pending;
            var pending = await Sum(filter, nameof(InvoiceEntity.TotalCost));

            return GenericResultModel<dynamic>.Ok(new { total, paid, pending, users });
        }

        public async Task<GridResultModel<object>> LoadGrid(InvoiceFilter filter, int draw, long userId, string userRole)
        {
            filter.ViewerId = userId;
            filter.ViewerRole = userRole;

            var invoices = await InvoiceRepository.Load(filter);
            var filtered = await Count(filter);
            var total = await Count(new InvoiceFilter
            {
                ViewerId = userId,
                ViewerRole = userRole
            });
            var users = await UserService.Load(
                invoices.Count == 0 ? [0] :
                userRole == UserRoleEnum.Company ? [.. invoices.Select(r => r.CustomerId)] :
                [.. invoices.Select(r => r.CompanyId)],
                [nameof(UserEntity.Id), nameof(UserEntity.Name), nameof(UserEntity.Avatar)]
            );

            return new GridResultModel<object>
            {
                Draw = draw,
                RecordsTotal = total,
                RecordsFiltered = filtered,
                Data = invoices.Select(i => new
                {
                    id = i.Id,
                    number = i.Number,
                    requestId = i.RequestId,
                    requestNumber = i.RequestNumber,
                    status = i.Status,
                    totalCost = i.TotalCost,
                    serviceCost = i.ServiceCost,
                    subtotalCost = i.SubtotalCost,
                    user = UserService.GetAvatar(users.FirstOrDefault(u => u.Id == i.CompanyId || u.Id == i.CustomerId))
                })
            };
        }
    }
}
