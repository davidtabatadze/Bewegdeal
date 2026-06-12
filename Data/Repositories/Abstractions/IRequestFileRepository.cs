using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IRequestFileRepository : IRepository
    {
        Task SetMain(long requestId, long fileId);
        Task<List<RequestFileEntity>> Load(long? requestId, List<long>? requestIds = null, bool? isMain = null);
    }
}
