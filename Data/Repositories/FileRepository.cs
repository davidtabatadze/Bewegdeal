using Bewegdeal.Data.Entities;
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

        public async Task<FileEntity?> Get(long id)
        {
            return await _context.Files.FirstOrDefaultAsync(f => f.Id == id);
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
