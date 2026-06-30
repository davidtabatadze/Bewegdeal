using Bewegdeal.Data.Base;
using Bewegdeal.Enums;

namespace Bewegdeal.Data.Entities
{
    public class RequestProposalEntity : IEntity
    {
        public long Id { get; set; }
        public long? ChatId { get; set; }
        public long RequestId { get; set; }
        public long CompanyId { get; set; }
        public long CustomerId { get; set; }
        public long InvoiceId { get; set; }
        public DateTime CreateDate { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; } = "EUR";
        public DateOnly? Date { get; set; }
        public TimeOnly? Time { get; set; }
        public string? ServiceTerms { get; set; }
        public string Status { get; set; } = RequestProposalStatusEnum.Pending;
        public DateTime? ReactionDate { get; set; }
        public string? ReactionReason { get; set; }
    }
}
