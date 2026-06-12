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
                new FraudWordEntity { Id = 1, Word = "mobile" },
                new FraudWordEntity { Id = 2, Word = "599*" },
                new FraudWordEntity { Id = 3, Word = "*bank" },
                new FraudWordEntity { Id = 4, Word = "*cash*" }
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
