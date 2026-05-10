using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;

namespace Bewegdeal.Data.Repositories
{
    /// <summary>
    /// Pure data-access contract for the Files table.
    /// Stores file metadata only — bytes are managed by IFileStorageService.
    /// </summary>
    public interface IFileRepository : IRepository
    {
        /// <summary>Returns the first file matching all non-null criteria, or null if none found.</summary>
        Task<FileEntity?> Get(FileFilter filter);

        /// <summary>Inserts a file metadata record and returns it with the generated Id.</summary>
        Task<FileEntity> Create(FileEntity file);

        /// <summary>Removes a file metadata record by Id.</summary>
        Task Delete(long id);
    }
}
