using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Filters
{
    public class FileFilter : BaseFilter<long?>
    {
        public string? Key { get; set; }
    }
}
