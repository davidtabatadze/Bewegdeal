using Bewegdeal.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bewegdeal.Models
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required]
        [MinLength(1)]
        [MaxLength(16)]
        public string Role { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        [MaxLength(32)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        [MaxLength(16)]
        public string Mobile { get; set; } = string.Empty;

        [MaxLength(16)]
        public string? Number { get; set; }

        [MaxLength(256)]
        public string? Address { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(32)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(16)]
        public string Password { get; set; } = string.Empty;

        public string Theme { get; set; } = UserThemeEnum.Light;

        public IFormFile? TermsFile { get; set; }

        public string[]? Interests { get; set; }

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

                if (Interests == null || Interests.Length == 0)
                {
                    yield return new ValidationResult(
                        "At least one interest is required for companies.",
                        [nameof(Interests)]
                    );
                }
                else if (Interests.Any(i => !ServiceEnum.All.Contains(i)))
                {
                    yield return new ValidationResult(
                        "Interests ins not valid for companies.",
                        [nameof(Interests)]
                    );
                }
            }
            else
            {
                Number = null;
                Address = null;
                Interests = [];
            }
        }
    }
}
