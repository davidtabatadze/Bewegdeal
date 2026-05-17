namespace Bewegdeal.Models
{
    public class RequestViewModel
    {
        public long Id { get; set; }
        public string Service { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public bool IsASAP { get; set; } = true;
        public string? Date { get; set; }
        public string? Time { get; set; }
        public IFormFile[]? Images { get; set; }
        public IFormFile[]? Videos { get; set; }
        public int MainImageIndex { get; set; }
        public long[] KeepFileIds { get; set; } = [];
        public long KeepMainFileId { get; set; }
    }
}
