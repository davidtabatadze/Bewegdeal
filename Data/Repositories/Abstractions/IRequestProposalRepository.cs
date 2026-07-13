using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IRequestProposalRepository : IRepository
    {
        Task Update(RequestProposalUpdateAreaEnum area, RequestProposalEntity update);
        Task<List<RequestProposalEntity>> Load(RequestProposalFilter filter, string[]? properties = null);
        Task<int> Count(RequestProposalFilter filter);
    }
}
