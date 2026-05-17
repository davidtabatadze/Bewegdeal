using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IFileRepository : IRepository
    {
        Task<FileEntity> Create(FileEntity file);
        Task<FileEntity?> Get(long id);
        Task<List<FileEntity>> Load(BaseFilter<long> filter);
        Task Delete(long id);
    }
}
