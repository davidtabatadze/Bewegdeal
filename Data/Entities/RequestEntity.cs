using Bewegdeal.Data.Base;
using Bewegdeal.Enums;

namespace Bewegdeal.Data.Entities
{
    public class RequestEntity : IEntity
    {
        public long Id { get; set; }
        public Guid Code { get; set; }
        public string Status { get; set; } = RequestStatusEnum.Pending;
        public string Service { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string SourceAddress { get; set; } = "";
        public string DestinationAddress { get; set; } = "";
        public long RequesterId { get; set; }
        public long? ExecutorId { get; set; }
        public decimal ProposedCost { get; set; }
        public string ProposedCurrency { get; set; } = "EUR";
        public DateOnly? ProposedDate { get; set; }
        public TimeOnly? ProposedTime { get; set; }
        public bool ProposedASAP { get; set; }
        public DateTime? AgreementDate { get; set; }
        public decimal? AgreedCost { get; set; }
        public string? AgreedCurrency { get; set; } = "EUR";
        public DateOnly? AgreedDate { get; set; }
        public TimeOnly? AgreedTime { get; set; }
    }
}
