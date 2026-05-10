using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="IFileRepository"/>.
    /// Scoped per request. Interacts with the database only — no business logic.
    /// </summary>
    public class FileRepository(SqlContext context) : IFileRepository
    {
        private readonly SqlContext _context = context;

        public async Task<FileEntity?> Get(FileFilter filter)
        {
            var query = _context.Files.AsQueryable();

            if (filter.Id.HasValue)
            {
                query = query.Where(f => f.Id == filter.Id.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Key))
            {
                query = query.Where(f => f.Key == filter.Key);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<FileEntity> Create(FileEntity file)
        {
            _context.Files.Add(file);
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task Delete(long id)
        {
            await _context.Files
                .Where(f => f.Id == id)
                .ExecuteDeleteAsync();
        }
    }
}
