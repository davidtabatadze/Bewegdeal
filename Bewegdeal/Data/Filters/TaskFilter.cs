using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class TaskFilter : BaseFilter<long?>
    {
        public long? UserId { get; set; }
    }
}
