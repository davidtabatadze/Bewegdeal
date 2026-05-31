using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class FraudWordFilter : BaseFilter
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
    }
}
