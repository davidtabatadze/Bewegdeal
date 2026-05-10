namespace Bewegdeal.Data.Base
{
    /// <summary>
    /// Represents a generic filter
    /// </summary>
    /// <typeparam name="T">The type of the Id</typeparam>
    public class BaseFilter<T>
    {
        public T? Id { get; set; }
        public string? SortField { get; set; }
        public string? SortDirection { get; set; }
        public int? Start { get; set; }
        public int? Length { get; set; }
    }
}
