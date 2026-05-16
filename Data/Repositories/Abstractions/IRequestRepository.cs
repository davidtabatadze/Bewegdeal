using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IRequestRepository : IRepository
    {
        Task<RequestEntity> Create(RequestEntity request);
        Task Update(RequestEntity request);
        Task<RequestEntity?> Get(long id);
    }
}
