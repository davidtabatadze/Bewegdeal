using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Enums;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IRequestProposalRepository : IRepository
    {
        Task Update(RequestProposalUpdateAreaEnum area, RequestProposalEntity update);
        Task<List<RequestProposalEntity>> Load(List<long>? requestIds, long? chatId, string? status);
    }
}
