using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    /// <summary>
    /// Represents a row in the Users table. Pure data object — no business logic.
    /// Role and Status values come from <c>UserRoleEnum</c> and <c>UserStatusEnum</c>.
    /// </summary>
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
        public long? TermsFileId { get; set; }
    }
}
