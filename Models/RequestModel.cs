using Bewegdeal.Data.Entities;

namespace Bewegdeal.Models
{
    public class RequestModel
    {
        public RequestEntity? Data { get; set; }
        public UserAvatarModel? Requester { get; set; }
        public SettingsEntity? Settings { get; set; }
        public ProposalCardModel? ProposalCard { get; set; }
        public UserAvatarModel? ProposalCompany { get; set; }
        public List<RequestFileModel> Files { get; set; } = [];
        public bool AllowResolve { get; set; }
    }
}
