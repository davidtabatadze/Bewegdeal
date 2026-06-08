using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class RequestFileRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IRequestFileRepository
    {

        public async Task SetMain(long requestId, long id)
        {
            await Context.RequestFiles
                         .Where(i => i.RequestId == requestId)
                         .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsMain, false));

            var main = await Context.RequestFiles
                                    .Where(i =>
                                        i.Type == RequestFileTypeEnum.Image &&
                                        i.Id == id
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

        public async Task<List<RequestFileEntity>> Load(long? requestId, List<long>? requestIds = null, bool? isMain = null)
        {
            var query = Context.RequestFiles.AsQueryable();

            if (requestId is not null)
            {
                query = query.Where(i => i.RequestId == requestId);
            }

            if (requestIds is not null)
            {
                query = query.Where(i => requestIds.Contains(i.RequestId));
            }

            if (isMain is not null)
            {
                query = query.Where(i => i.IsMain == isMain);
            }

            return await query.ToListAsync();
        }
    }
}