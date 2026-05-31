namespace Bewegdeal.Models
{
    public class ResultModel
    {
        public bool Success { get; set; }

        public long? ObjectId { get; set; }

        // [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }

        public static ResultModel Ok(long objectId) =>
            new() { Success = true, ObjectId = objectId };

        public static ResultModel Ok(string? message = null) =>
            new() { Success = true, Message = message };

        public static ResultModel Fail(string? message = null) =>
            new() { Success = false, Message = message };
    }
}
