using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class InvoiceEntity : IEntity
    {
        public long Id { get; set; }
        public string Number { get; set; } = "";
        public string Status { get; set; } = "";
        public string Service { get; set; } = "";
        public string RequestNumber { get; set; } = "";
        public long RequestId { get; set; }
        public long ProposalId { get; set; }
        public long CompanyId { get; set; }
        public long CustomerId { get; set; }
        public string Currency { get; set; } = "EUR";
        public decimal ServiceCost { get; set; }
        public short CommissionPersent { get; set; }
        public decimal CommissionCost { get; set; }
        public short TaxPersent { get; set; }
        public decimal TaxCost { get; set; }
        public decimal TotalCost { get; set; }
        public bool NotificationSent { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
