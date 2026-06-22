using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class UserRatingEntity : IEntity
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long EvaluatorId { get; set; }
        public decimal Value { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
