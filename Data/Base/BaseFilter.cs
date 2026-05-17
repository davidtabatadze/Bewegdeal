namespace Bewegdeal.Data.Base
{
    public class BaseFilter<T>
    {
        public T? Id { get; set; }
        public List<T>? Ids { get; set; }
        public string? SortField { get; set; }
        public string? SortDirection { get; set; }
        public int? Start { get; set; }
        public int? Length { get; set; }
    }
}
