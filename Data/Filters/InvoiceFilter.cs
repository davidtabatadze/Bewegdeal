using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class InvoiceFilter : BaseFilter
    {
        public long? RequestId { get; set; }
        public string? Number { get; set; }
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? ViewerRole { get; set; }
        public long? ViewerId { get; set; }
        public decimal? AmountFrom { get; set; }
        public decimal? AmountTo { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
