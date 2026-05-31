using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class UserFilter : BaseFilter<long?>
    {
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? Search { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
    }
}
