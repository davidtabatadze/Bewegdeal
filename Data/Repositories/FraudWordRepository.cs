using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;

namespace Bewegdeal.Data.Repositories
{
    public class FraudWordRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IFraudWordRepository, IRepositorySeedable
    {
        public async Task Seed()
        {
            var rows = new[]
            {
                new FraudWordEntity { Id = 1, Word = "test-fraud" }
            };

            foreach (var row in rows)
            {
                if (await Get<FraudWordEntity>(row.Id) == null)
                {
                    await Create(row);
                }
            }
        }
    }
}
