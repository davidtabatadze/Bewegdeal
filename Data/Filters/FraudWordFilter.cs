using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class FraudWordFilter : BaseFilter<long?>
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
    }
}
