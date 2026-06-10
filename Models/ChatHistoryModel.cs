using Bewegdeal.Data.Entities;

namespace Bewegdeal.Models
{
    public class ChatHistoryModel
    {
        public string Mode { get; set; } = string.Empty;
        public string ChatKey { get; set; } = string.Empty;
        public string OtherPartyName { get; set; } = string.Empty;
        public string OtherPartyInitials { get; set; } = string.Empty;
        public string? OtherPartyPictureUrl { get; set; }
        public long ViewerId { get; set; }
        public string ViewerInitials { get; set; } = string.Empty;
        public string? ViewerPictureUrl { get; set; }
        public List<ChatMessageEntity> Messages { get; set; } = [];
    }
}
