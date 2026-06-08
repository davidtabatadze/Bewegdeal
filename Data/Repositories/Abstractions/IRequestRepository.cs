using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IRequestRepository : IRepository
    {
        Task<RequestEntity?> Get(RequestFilter filter, string[]? properties = null);
        Task<int> Count(RequestFilter filter);
        Task<List<RequestEntity>> Load(RequestFilter filter);
    }
}
