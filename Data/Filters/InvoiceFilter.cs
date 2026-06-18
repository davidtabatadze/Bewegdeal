using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class InvoiceFilter : BaseFilter
    {
        public string? Number { get; set; }
        public string? Search { get; set; }
        public string? Status { get; set; }
    }
}
