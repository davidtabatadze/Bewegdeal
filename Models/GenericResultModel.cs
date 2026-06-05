namespace Bewegdeal.Models
{
    public class GenericResultModel<T>
    {
        public bool Success { get; set; }
        public T? Result { get; set; }
        public string? Message { get; set; }

        public static GenericResultModel<T> Ok(T result, string? message = null)
            => new() { Success = true, Result = result, Message = message };
        public static GenericResultModel<T> Fail(T result, string? message = null)
            => new() { Success = false, Result = result, Message = message };
        public static GenericResultModel<T> Fail(string? message = null)
            => new() { Success = false, Message = message };
    }

    public class GenericResultModel
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public static GenericResultModel Ok(string? message = null)
            => new() { Success = true, Message = message };
        public static GenericResultModel Fail(string? message = null)
            => new() { Success = false, Message = message };
    }
}
