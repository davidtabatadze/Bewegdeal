namespace Bewegdeal.Models
{
    /// <summary>
    /// Server-side DataTables response envelope.
    /// Serializes to the exact shape DataTables expects:
    /// { draw, recordsTotal, recordsFiltered, data }.
    /// </summary>
    public class DataTablesResult<T>
    {
        public int Draw { get; init; }
        public int RecordsTotal { get; init; }
        public int RecordsFiltered { get; init; }
        public IEnumerable<T> Data { get; init; }

        public DataTablesResult(int draw, int recordsTotal, int recordsFiltered, IEnumerable<T> data)
        {
            Draw = draw;
            RecordsTotal = recordsTotal;
            RecordsFiltered = recordsFiltered;
            Data = data;
        }
    }
}
