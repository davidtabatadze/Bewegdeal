using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;

namespace Bewegdeal.Services
{
    public class ProposalService(IRequestProposalRepository ProposalRepository)
    {
        public async Task<RequestProposalEntity> Create(RequestProposalEntity proposal)
            => await ProposalRepository.Create(proposal);
        public async Task<RequestProposalEntity?> GetActual(long chatId)
            => (await Load(new RequestProposalFilter { ChatId = chatId, Status = RequestProposalStatusEnum.Pending })).FirstOrDefault();
        public async Task<RequestProposalEntity?> Get(long id, string[]? properties = null)
            => await ProposalRepository.Get<RequestProposalEntity>(id, properties);
        public async Task<List<RequestProposalEntity>> Load(long chatId)
            => await Load(new RequestProposalFilter { ChatId = chatId });
        public async Task<List<RequestProposalEntity>> Load(List<long> requestIds)
            => await Load(new RequestProposalFilter { RequestIds = requestIds });
        public async Task<List<RequestProposalEntity>> Load(RequestProposalFilter filter)
            => await ProposalRepository.Load(filter);
        public async Task Update(long id, long invoiceId)
            => await ProposalRepository.Update(
                    RequestProposalUpdateAreaEnum.Invoice,
                    new RequestProposalEntity
                    {
                        Id = id,
                        InvoiceId = invoiceId
                    }
               );
        public async Task Update(long id, string status, string? reason = null)
            => await ProposalRepository.Update(
                    RequestProposalUpdateAreaEnum.Status,
                    new RequestProposalEntity
                    {
                        Id = id,
                        Status = status,
                        ReactionReason = reason
                    }
               );
    }
}
