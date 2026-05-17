using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IRequestFileRepository : IRepository
    {
        Task Create(List<RequestFileEntity> files);
        Task SetMainImage(long requestId, long fileId);
        Task<List<RequestFileEntity>> Load(long requestId);
        Task<List<RequestFileEntity>> LoadMainImages(List<long> requestIds);
        Task Delete(List<long> ids);
    }
}
