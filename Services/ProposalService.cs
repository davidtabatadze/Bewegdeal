using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;

namespace Bewegdeal.Services
{
    public class ProposalService(IRequestProposalRepository ProposalRepository)
    {
        public async Task<RequestProposalEntity> Create(RequestProposalEntity proposal)
            => await ProposalRepository.Create(proposal);
        public async Task<RequestProposalEntity?> GetActual(long chatId)
            => (await Load(chatId, RequestProposalStatusEnum.Pending)).FirstOrDefault();
        public async Task<RequestProposalEntity?> Get(long id, string[]? properties = null)
            => await ProposalRepository.Get<RequestProposalEntity>(id, properties);
        public async Task<List<RequestProposalEntity>> Load(long? chatId, string? status)
            => await ProposalRepository.Load(null, chatId, status);
        public async Task Update(long id, string status, string? reason = null)
            => await ProposalRepository.Update(
                    RequestProposalUpdateAreaEnum.Status,
                    new RequestProposalEntity
                    {
                        Id = id,
                        ReactionReason = reason,
                        Status = status
                    }
               );
    }
}
