using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;

namespace Bewegdeal.Data.Repositories
{
    public interface IRequestRepository : IRepository
    {
        Task<RequestEntity> Create(RequestEntity request);
    }
}
