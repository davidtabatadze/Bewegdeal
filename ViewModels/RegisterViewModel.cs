using Bewegdeal.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bewegdeal.ViewModels
{
    public class RegistrationViewModel : IValidatableObject
    {
        [Required]
        [MinLength(1)]
        [MaxLength(16)]
        public string Role { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        [MaxLength(16)]
        public string Mobile { get; set; } = string.Empty;

        [MaxLength(16)]
        public string? Number { get; set; }

        [MaxLength(256)]
        public string? Address { get; set; }

        [MaxLength(128)]
        public string? Owner { get; set; }

        [MaxLength(64)]
        public string? City { get; set; }

        [MaxLength(8)]
        public string? ZipCode { get; set; }

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
                    yield return new ValidationResult(AnnotationEnum.Account.Profile.UidRequired, [nameof(Number)]);
                }
                if (string.IsNullOrWhiteSpace(Address))
                {
                    yield return new ValidationResult(AnnotationEnum.Account.Profile.AddressRequired, [nameof(Address)]);
                }
                if (string.IsNullOrWhiteSpace(Owner))
                {
                    yield return new ValidationResult(AnnotationEnum.Account.Profile.OwnerRequired, [nameof(Owner)]);
                }
                if (string.IsNullOrWhiteSpace(City))
                {
                    yield return new ValidationResult(AnnotationEnum.Account.Profile.CityRequired, [nameof(City)]);
                }
                if (string.IsNullOrWhiteSpace(ZipCode))
                {
                    yield return new ValidationResult(AnnotationEnum.Account.Profile.ZipCodeRequired, [nameof(ZipCode)]);
                }

                if (Interests == null || Interests.Length == 0)
                {
                    yield return new ValidationResult(AnnotationEnum.Account.Profile.InterestsRequired, [nameof(Interests)]);
                }
                else if (Interests.Any(i => !ServiceEnum.All.Contains(i)))
                {
                    yield return new ValidationResult(AnnotationEnum.Account.Profile.InterestsInvalid, [nameof(Interests)]);
                }
            }
            else
            {
                Number = null;
                Address = null;
                Owner = null;
                City = null;
                ZipCode = null;
                Interests = [];
            }
        }
    }
}
