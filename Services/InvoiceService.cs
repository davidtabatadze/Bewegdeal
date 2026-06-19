using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;

namespace Bewegdeal.Services
{
    public class InvoiceService(IInvoiceRepository InvoiceRepository)
    {
        public async Task<InvoiceEntity> Create(InvoiceEntity invoice)
            => await InvoiceRepository.Create(invoice);
        public async Task Update(InvoiceUpdateAreaEnum area, InvoiceEntity update)
            => await InvoiceRepository.Update(area, update);
        public async Task<InvoiceEntity?> Get(InvoiceFilter filter, string[]? properties = null)
            => await InvoiceRepository.Get(filter, properties);
    }
}
