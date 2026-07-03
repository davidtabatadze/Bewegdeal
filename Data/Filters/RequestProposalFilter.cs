using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class RequestProposalFilter : BaseFilter
    {
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public string? Status { get; set; }
        public long? ChatId { get; set; }
        public long? InvoiceId { get; set; }
        public long? CompanyId { get; set; }
        public List<long>? RequestIds { get; set; }
    }
}
