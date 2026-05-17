using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class RequestFilter : BaseFilter<long?>
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Service { get; set; }
    }
}
