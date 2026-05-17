using Bewegdeal.Data.Base;
using Bewegdeal.Enums;

namespace Bewegdeal.Data.Entities
{
    public class RequestAgreementEntity : IEntity
    {
        public long Id { get; set; }
        public DateTime CreateDate { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; } = "EUR";
        public DateOnly? Date { get; set; }
        public TimeOnly? Time { get; set; }
        public long? ServiceTermsFileId { get; set; }
        public string Status { get; set; } = RequestAgreementStatusEnum.Pending;
        public DateTime? ReactionDate { get; set; }
        public string? ReactionReason { get; set; }
    }
}
