namespace Bewegdeal.Data.Base
{
    /// <summary>
    /// Represents a generic filter
    /// </summary>
    /// <typeparam name="T">The type of the Id</typeparam>
    public class BaseFilter<T>
    {
        public T? Id { get; set; }
    }
}
