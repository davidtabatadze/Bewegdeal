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

        public async Task<GridResultModel<object>> LoadGrid(InvoiceFilter filter, int draw, long userId)
        {
            var user = await UserService.Get(userId,
                [nameof(UserEntity.Id), nameof(UserEntity.Role)]
            );
            filter.ViewerId = user?.Id ?? 0;
            filter.ViewerRole = user?.Role ?? "-";

            var invoices = await InvoiceRepository.Load(filter);
            var filtered = await InvoiceRepository.Count(filter);
            var total = await InvoiceRepository.Count(new InvoiceFilter
            {
                ViewerId = user?.Id ?? 0,
                ViewerRole = user?.Role ?? "-"
            });
            var users = await UserService.Load(
                invoices.Count == 0 ? [0] :
                user?.Role == UserRoleEnum.Company ? [.. invoices.Select(r => r.CustomerId)] :
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
