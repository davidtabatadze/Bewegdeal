using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class RequestFilter : BaseFilter
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Service { get; set; }
        public string? ViewerRole { get; set; }
        public long? ViewerId { get; set; }
        public string? ViewerFocus { get; set; }
        public string[] ViewerInterests { get; set; } = [];
    }
}
