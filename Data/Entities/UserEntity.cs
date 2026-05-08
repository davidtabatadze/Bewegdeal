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

        /// <summary>See <c>UserRoleEnum</c> for valid values.</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>See <c>UserStatusEnum</c> for valid values.</summary>
        public string Status { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;

        /// <summary>PBKDF2/SHA-256 hash — produced by <c>PasswordTool.HashPassword</c>.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Random salt paired with <see cref="Password"/>.</summary>
        public string Salt { get; set; } = string.Empty;

        /// <summary>Short-lived code used for email verification or password reset.</summary>
        public string? Code { get; set; }

        /// <summary>Company identification / registration number.</summary>
        public string? Number { get; set; }

        public string? Address { get; set; }

        /// <summary>
        /// Service interests. Valid values come from <c>ServiceEnum</c>.
        /// Stored in the database as a comma-separated string.
        /// </summary>
        public string[] Interests { get; set; } = [];
    }
}
