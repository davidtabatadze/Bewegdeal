using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class FraudWordEntity : IEntity
    {
        public long Id { get; set; }
        public string Word { get; set; } = string.Empty;
    }
}
