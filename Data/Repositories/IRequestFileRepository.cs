using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;

namespace Bewegdeal.Data.Repositories
{
    public interface IRequestFileRepository : IRepository
    {
        Task<RequestFileEntity> Create(RequestFileEntity file);
        Task<List<RequestFileEntity>> Load(long requestId);
        Task Delete(long requestId);
    }
}
