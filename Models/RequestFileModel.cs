namespace Bewegdeal.Models
{
    public class RequestFileModel
    {
        public long Id { get; set; }
        public long Size { get; set; }
        public bool IsMain { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
