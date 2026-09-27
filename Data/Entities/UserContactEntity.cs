using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class UserContactEntity : IEntity
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string? Address { get; set; }
        public string? Owner { get; set; }
        public string? City { get; set; }
        public string? ZipCode { get; set; }
        public string? ServiceTerms { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
