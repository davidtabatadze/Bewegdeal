using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class RequestFilter : BaseFilter<long?>
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Service { get; set; }
        public string? ViewerRole { get; set; }
        public long? ViewerId { get; set; }
        public long? ExecutorId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
