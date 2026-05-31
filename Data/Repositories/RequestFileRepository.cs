using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class RequestFileRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IRequestFileRepository
    {

        // ── Write ────────────────────────────────────────────────────────────────

        public async Task Create(List<RequestFileEntity> files)
        {
            Context.RequestFiles.AddRange(files);
            await Context.SaveChangesAsync();
        }

        public async Task SetMainImage(long requestId, long fileId)
        {
            await Context.RequestFiles
                         .Where(i => i.RequestId == requestId)
                         .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsMain, false));

            var main = await Context.RequestFiles
                                    .Where(i =>
                                        i.Type == RequestFileTypeEnum.Image &&
                                        i.RequestId == requestId &&
                                        i.FileId == fileId
                                    )
                                    .FirstOrDefaultAsync();
            main ??= await Context.RequestFiles
                                  .Where(i =>
                                      i.Type == RequestFileTypeEnum.Image &&
                                      i.RequestId == requestId
                                  )
                                  .OrderBy(i => i.Id)
                                  .FirstOrDefaultAsync();

            if (main is not null)
            {
                main.IsMain = true;
                await Context.SaveChangesAsync();
            }
        }

        // ── Read ─────────────────────────────────────────────────────────────────

        public async Task<List<RequestFileEntity>> Load(long requestId)
        {
            return await Context.RequestFiles
                                .Where(i => i.RequestId == requestId)
                                .ToListAsync();
        }

        public async Task<List<RequestFileEntity>> LoadMainImages(List<long> requestIds)
        {
            return await Context.RequestFiles
                                .Where(f => requestIds.Contains(f.RequestId) && f.IsMain)
                                .ToListAsync();
        }

        // ── Delete ───────────────────────────────────────────────────────────────

        public async Task Delete(List<long> ids)
        {
            await Context.RequestFiles
                         .Where(i => ids.Contains(i.Id))
                         .ExecuteDeleteAsync();
        }
    }
}
