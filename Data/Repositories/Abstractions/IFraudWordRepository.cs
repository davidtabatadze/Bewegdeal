using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IFraudWordRepository : IRepository
    {
        Task<FraudWordEntity?> Get(FraudWordFilter filter);
        Task<List<FraudWordEntity>> Load(FraudWordFilter filter);
        Task<int> Count(FraudWordFilter filter);
        Task Update(long id, string word, string description);
        Task SetStatus(long id, string status);
    }
}
