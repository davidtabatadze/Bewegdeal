using Bewegdeal.Data.Entities;

namespace Bewegdeal.Models
{
    public class ProposalCardModel
    {
        public RequestProposalEntity? Proposal { get; set; }
        public UserContactEntity? CompanyContact { get; set; }
    }
}
