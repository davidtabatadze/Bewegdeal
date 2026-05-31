using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class UserEntity : IEntity
    {
        public long Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public string? Number { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public string[] Interests { get; set; } = [];
        public long? ServiceTermsFileId { get; set; }
        public long? ProfilePictureFileId { get; set; }
        public string Theme { get; set; } = "light";
        public bool AcquaintedHIW { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime TermsAndConditionsAcceptDate { get; set; }
    }
}
