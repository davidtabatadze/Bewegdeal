using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IInvoiceRepository : IRepository
    {
        Task Update(InvoiceUpdateAreaEnum area, InvoiceEntity update);
        Task<InvoiceEntity?> Get(InvoiceFilter filter, string[]? properties = null);
        Task<int> Count(InvoiceFilter filter);
        Task<List<InvoiceEntity>> Load(InvoiceFilter filter);
    }
}
