namespace Bewegdeal.Data.Base
{
    public class BaseFilter
    {
        public long? Id { get; set; }
        public List<long>? Ids { get; set; }
        public string? SortField { get; set; }
        public string? SortDirection { get; set; }
        public int? Start { get; set; }
        public int? Length { get; set; }
    }
}
