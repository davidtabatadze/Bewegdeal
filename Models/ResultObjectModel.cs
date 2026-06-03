namespace Bewegdeal.Models
{
    public class ResultObjectModel<T>
    {
        public bool Success { get; set; }
        public T? Object { get; set; }
        public string? Message { get; set; }
        public static ResultObjectModel<T> Ok(T result) =>
            new() { Success = true, Object = result };
        public static ResultObjectModel<T> Ok(string? message = null) =>
            new() { Success = true, Message = message };
        public static ResultObjectModel<T> Fail(string? message = null) =>
            new() { Success = false, Message = message };
        public static ResultObjectModel<T> Fail(T result, string? message = null) =>
            new() { Success = false, Object = result, Message = message };
    }
}
