using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class FileRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IFileRepository
    {

        public async Task<List<FileEntity>> Load(BaseFilter filter)
        {
            filter.Ids ??= [0];
            return await Context.Files.Where(i => filter.Ids.Contains(i.Id))
                                      .ToListAsync();
        }

    }
}