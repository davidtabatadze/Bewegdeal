using System.ComponentModel.DataAnnotations;
using Bewegdeal.Enums;

namespace Bewegdeal.Models
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required]
        [MaxLength(16)]
        public string Role { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(16)]
        public string Mobile { get; set; } = string.Empty;

        // Max length mirrors SqlContext ConfigureUsers
        [MaxLength(16)]
        public string? Number { get; set; }

        [MaxLength(256)]
        public string? Address { get; set; }

        [Required]
        [MaxLength(32)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(16)]
        public string Password { get; set; } = string.Empty;

        // Each nullable property represents one service checkbox.
        // null = unchecked; non-null = checked (value is the ServiceEnum constant).
        // Company only — Customer always results in an empty Interests array.
        public string? ServiceMoving { get; set; }
        public string? ServiceJunk { get; set; }
        public string? ServiceStorePickup { get; set; }
        public string? ServiceVehicle { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Role == UserRoleEnum.Company)
            {
                if (string.IsNullOrWhiteSpace(Number))
                {
                    yield return new ValidationResult(
                        "Identification number is required for companies.",
                        [nameof(Number)]
                    );
                }

                if (string.IsNullOrWhiteSpace(Address))
                {
                    yield return new ValidationResult(
                        "Address is required for companies.",
                        [nameof(Address)]
                    );
                }
            }
        }
    }
}
