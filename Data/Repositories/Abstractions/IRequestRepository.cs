using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IRequestRepository : IRepository
    {
        Task<RequestEntity> Create(RequestEntity request);
        Task Update(RequestEntity request);
        Task<RequestEntity?> Get(long id);
        Task<RequestEntity?> Get(string number);
        Task<int> Count(RequestFilter filter);
        Task<List<RequestEntity>> Load(RequestFilter filter);
    }
}
