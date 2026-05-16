using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class RequestFileRepository(SqlContext context) : IRequestFileRepository
    {

        // ── Write ────────────────────────────────────────────────────────────────

        public async Task Create(List<RequestFileEntity> files)
        {
            context.RequestFiles.AddRange(files);
            await context.SaveChangesAsync();
        }

        public async Task SetMainImage(long requestId, long fileId)
        {
            var main = await context.RequestFiles
                                    .Where(i =>
                                        i.Type == RequestFileTypeEnum.Image &&
                                        i.RequestId == requestId &&
                                        i.FileId == fileId
                                    )
                                    .FirstOrDefaultAsync();
            main ??= await context.RequestFiles
                                  .Where(i =>
                                      i.Type == RequestFileTypeEnum.Image &&
                                      i.RequestId == requestId
                                  )
                                  .OrderBy(i => i.Id)
                                  .FirstOrDefaultAsync();

            if (main is not null)
            {
                main.IsMain = true;
                await context.SaveChangesAsync();
            }
        }

        // ── Read ─────────────────────────────────────────────────────────────────

        public async Task<List<RequestFileEntity>> Load(long requestId)
        {
            return await context.RequestFiles
                                .Where(i => i.RequestId == requestId)
                                .ToListAsync();
        }

        // ── Delete ───────────────────────────────────────────────────────────────

        public async Task Delete(List<long> ids)
        {
            await context.RequestFiles
                         .Where(i => ids.Contains(i.Id))
                         .ExecuteDeleteAsync();
        }
    }
}
