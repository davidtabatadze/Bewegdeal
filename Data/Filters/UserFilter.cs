using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class UserFilter : BaseFilter
    {
        public string? Email { get; set; }
        public string? Search { get; set; }
        public string? Role { get; set; }
        public string? ExcludeRole { get; set; }
        public string? Status { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
