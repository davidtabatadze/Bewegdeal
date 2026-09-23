using Bewegdeal.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bewegdeal.ViewModels
{
    public class ProfileViewModel : IValidatableObject
    {
        [Required]
        [MinLength(1)]
        [MaxLength(16)]
        public string Role { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        [MaxLength(32)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? Address { get; set; }

        [MaxLength(128)]
        public string? Owner { get; set; }

        [MaxLength(64)]
        public string? City { get; set; }

        [MaxLength(8)]
        public string? ZipCode { get; set; }

        public string[]? Interests { get; set; }

        public IFormFile? ServiceTermsFile { get; set; }

        public bool DeleteServiceTerms { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Role == UserRoleEnum.Company)
            {
                if (string.IsNullOrWhiteSpace(Owner))
                {
                    yield return new ValidationResult(
                        "Owner is required for companies.",
                        [nameof(Owner)]
                    );
                }
                if (string.IsNullOrWhiteSpace(Address))
                {
                    yield return new ValidationResult(
                        "Address is required for companies.",
                        [nameof(Address)]
                    );
                }
                if (string.IsNullOrWhiteSpace(City))
                {
                    yield return new ValidationResult(
                        "City is required for companies.",
                        [nameof(City)]
                    );
                }
                if (string.IsNullOrWhiteSpace(ZipCode))
                {
                    yield return new ValidationResult(
                        "Zip code is required for companies.",
                        [nameof(ZipCode)]
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
                Owner = null;
                Address = null;
                City = null;
                ZipCode = null;
                Interests = [];
            }
        }
    }
}
