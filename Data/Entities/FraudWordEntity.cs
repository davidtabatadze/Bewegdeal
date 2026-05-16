using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class FraudWordEntity : IEntity
    {
        public long Id { get; set; }
        public string Word { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
    }
}
