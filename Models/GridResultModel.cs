namespace Bewegdeal.Models
{
    public class GridResultModel<T>
    {
        public int Draw { get; init; }
        public int RecordsTotal { get; init; }
        public int RecordsFiltered { get; init; }
        public IEnumerable<T> Data { get; init; } = [];
    }
}
