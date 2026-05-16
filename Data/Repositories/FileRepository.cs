using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class FileRepository(SqlContext context) : IFileRepository
    {

        // ── Write ────────────────────────────────────────────────────────────────

        public async Task<FileEntity> Create(FileEntity file)
        {
            context.Files.Add(file);
            await context.SaveChangesAsync();
            return file;
        }

        // ── Read ─────────────────────────────────────────────────────────────────

        public async Task<FileEntity?> Get(long id)
        {
            return await context.Files.FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<List<FileEntity>> Load(BaseFilter<long> filter)
        {
            filter.Ids ??= [0];

            return await context.Files
                                .Where(i => filter.Ids.Contains(i.Id))
                                .ToListAsync();
        }

        // ── Delete ───────────────────────────────────────────────────────────────

        public async Task Delete(long id)
        {
            await context.Files
                .Where(f => f.Id == id)
                .ExecuteDeleteAsync();
        }

    }
}
