using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
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
            await Context.RequestFiles
                         .Where(i => i.RequestId == requestId && i.Id == id)
                         .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsMain, true));
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