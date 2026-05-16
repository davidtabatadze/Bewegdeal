namespace Bewegdeal.Models
{
    public class RequestViewModel
    {
        public long Id { get; set; }           // 0 on Create
        public string Service { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string SourceAddress { get; set; } = string.Empty;
        public string DestinationAddress { get; set; } = string.Empty;
        public decimal ProposedCost { get; set; }
        public bool IsASAP { get; set; } = true;
        public string? ProposedDate { get; set; }
        public string? ProposedTime { get; set; }
        public IFormFile[]? Images { get; set; }
        public IFormFile[]? Videos { get; set; }
        public int MainImageIndex { get; set; }
        public long[] KeepFileIds { get; set; } = [];     // empty on Create
        public long KeepMainFileId { get; set; }           // 0 on Create = main is a new image
    }
}
