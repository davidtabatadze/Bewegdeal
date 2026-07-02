using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Models;

namespace Bewegdeal.Services
{
    public class InvoiceService(IInvoiceRepository InvoiceRepository, UserService UserService, SettingService SettingService)
    {
        public async Task<InvoiceEntity> Create(RequestEntity request, RequestProposalEntity proposal)
        {
            var settings = await SettingService.GetCached();
            var commision = proposal.Cost / 100 * settings.InvoiceCommissionPersent;
            var tax = commision / 100 * settings.InvoiceTaxPersent;

            return await InvoiceRepository.Create(new InvoiceEntity
            {
                Number = Guid.NewGuid().ToString("N"),
                Status = InvoiceStatusEnum.Pending,
                Service = request.Service,
                RequestId = request.Id,
                RequestNumber = request.Number,
                ProposalId = proposal.Id,
                CompanyId = proposal.CompanyId,
                CustomerId = request.RequesterId,
                Currency = proposal.Currency,

                TaxPersent = settings.InvoiceTaxPersent,
                CommissionPersent = settings.InvoiceCommissionPersent,

                TaxCost = tax,
                CommissionCost = commision,
                ServiceCost = proposal.Cost,
                TotalCost = commision + tax,

                NotificationSent = false,
                CreateDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(settings.InvoiceDueDays)
            });
        }
        public async Task Update(InvoiceUpdateAreaEnum area, InvoiceEntity update)
            => await InvoiceRepository.Update(area, update);
        public async Task<InvoiceEntity?> Get(long id, string[]? properties = null)
            => await InvoiceRepository.Get<InvoiceEntity>(id, properties);
        public async Task<List<InvoiceEntity>> Load(InvoiceFilter filter)
            => await InvoiceRepository.Load(filter);
        private async Task<decimal> Sum(InvoiceFilter filter, string property)
            => await InvoiceRepository.Sum(filter, property);
        private async Task<int> Count(InvoiceFilter filter)
            => await InvoiceRepository.Count(filter);
        private async Task<int> CountDistinct(InvoiceFilter filter, string property)
            => await InvoiceRepository.CountDistinct(filter, property);

        public async Task<GenericResultModel<InvoicePrintModel>> Get(string number, long userId, string userRole)
        {
            var invoice = await InvoiceRepository.Get(new InvoiceFilter
            {
                Number = number,
                ViewerId = userId,
                ViewerRole = userRole
            });

            var company = await UserService.Get(
                invoice?.CompanyId ?? 0,
                [nameof(UserEntity.Number), nameof(UserEntity.Name), nameof(UserEntity.Address),
                    nameof(UserEntity.Mobile), nameof(UserEntity.Email)]
            );

            if (invoice is null || company is null)
            {
                return GenericResultModel<InvoicePrintModel>.Fail();
            }

            return GenericResultModel<InvoicePrintModel>.Ok(new InvoicePrintModel
            {
                Data = invoice,
                Company = company
            });
        }

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

            var invoices = await Load(filter);
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
                    createDate = i.CreateDate,
                    dueDate = i.DueDate,
                    paymentDate = i.PaymentDate,
                    totalCost = i.TotalCost,
                    serviceCost = i.ServiceCost,
                    subtotalCost = i.CommissionCost,
                    user = UserService.GetAvatar(users.FirstOrDefault(u => u.Id == i.CompanyId || u.Id == i.CustomerId))
                })
            };
        }
    }
}
