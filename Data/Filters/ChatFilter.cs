using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class ChatFilter : BaseFilter
    {
        public string? Key { get; set; }
        public string? Status { get; set; }
        public string? Fraud { get; set; }
        public string? Search { get; set; }
        public long? RequestId { get; set; }
    }
}
