using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    /// <summary>
    /// Criteria bag for user queries. Only non-null fields are applied.
    /// Add new fields here as lookup needs grow — no new repository methods required.
    /// </summary>
    public class UserFilter : BaseFilter<long?>
    {
        public string? Email { get; set; }
        public string? Search { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
    }
}
