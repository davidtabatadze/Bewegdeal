using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class ChatEntity : IEntity
    {
        public long Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public long RequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public long CustomerId { get; set; }
        public long CompanyId { get; set; }
        public string Fraud { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
    }
}
