using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IFileRepository : IRepository
    {
        Task<List<FileEntity>> Load(BaseFilter filter);
    }
}
