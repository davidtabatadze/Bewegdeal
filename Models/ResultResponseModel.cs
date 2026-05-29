using System.Text.Json.Serialization;

namespace Bewegdeal.Models
{
    public class ResultResponseModel
    {
        public bool Success { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Error { get; set; }

        public static ResultResponseModel Ok() =>
            new() { Success = true };

        public static ResultResponseModel Fail(string? error = null) =>
            new() { Success = false, Error = error };
    }
}
