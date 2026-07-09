using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IRequestRepository : IRepository
    {
        Task Update(RequestUpdateAreaEnum area, RequestEntity update);
        Task<RequestEntity?> Get(RequestFilter filter, string[]? properties = null);
        Task<int> Count(RequestFilter filter);
        Task<List<RequestEntity>> Load(RequestFilter filter);
    }
}
