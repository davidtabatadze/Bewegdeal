using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class ChatMessageEntity : IEntity
    {
        public long Id { get; set; }
        public long ChatId { get; set; }
        public long SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentDate { get; set; }
        public bool IsRead { get; set; }
    }
}
