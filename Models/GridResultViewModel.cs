namespace Bewegdeal.Models
{
    /// <summary>
    /// Server-side DataTables response envelope.
    /// Serializes to the exact shape DataTables expects:
    /// { draw, recordsTotal, recordsFiltered, data }.
    /// </summary>
    public class GridResultViewModel<T>(int draw, int recordsTotal, int recordsFiltered, IEnumerable<T> data)
    {
        public int Draw { get; init; } = draw;
        public int RecordsTotal { get; init; } = recordsTotal;
        public int RecordsFiltered { get; init; } = recordsFiltered;
        public IEnumerable<T> Data { get; init; } = data;
    }
}
