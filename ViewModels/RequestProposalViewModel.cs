using Bewegdeal.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bewegdeal.ViewModels
{
    public class RequestProposalViewModel : IValidatableObject
    {
        public long RequestId { get; set; }

        [Required]
        public string RequestNumber { get; set; } = string.Empty;

        [Required]
        public decimal Cost { get; set; }

        [Required]
        public string Currency { get; set; } = "EUR";

        [Required]
        public string? Date { get; set; }

        [Required]
        public string? Time { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var field =
                (Cost < 1 || Cost > 10000) ? AnnotationEnum.Request.Requirement.Cost :
                (!DateOnly.TryParse(Date, out _)) ? AnnotationEnum.Request.Requirement.Date :
                (!TimeOnly.TryParse(Time, out _)) ? AnnotationEnum.Request.Requirement.Time :
                null;

            if (field is not null)
            {
                yield return new ValidationResult(
                    string.Format(AnnotationEnum.Request.Requirement.Error, field)
                );
            }
        }

    }
}
