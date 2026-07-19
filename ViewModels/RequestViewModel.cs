using Bewegdeal.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bewegdeal.ViewModels
{
    public class RequestViewModel : IValidatableObject
    {

        public long Id { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(16)]
        public string Service { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        [MaxLength(64)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2048)]
        public string? Description { get; set; }

        [MaxLength(512)]
        public string? PickupAddress { get; set; }

        [MaxLength(8)]
        public string? PickupZipCode { get; set; }

        [MaxLength(512)]
        public string? DeliveryAddress { get; set; }

        [MaxLength(8)]
        public string? DeliveryZipCode { get; set; }

        [Required]
        public decimal Cost { get; set; }

        [Required]
        public bool IsASAP { get; set; } = true;

        public string? Date { get; set; }
        public string? Time { get; set; }
        public IFormFile[]? Images { get; set; } = [];
        public IFormFile[]? Videos { get; set; } = [];
        public int MainImageIndex { get; set; }
        public long[] KeepFileIds { get; set; } = [];
        public long KeepMainFileId { get; set; }
        public string? VehicleType { get; set; }
        public string? VehicleCondition { get; set; }
        public bool PresentElevator { get; set; }
        public bool PresentParking { get; set; }

        #region Validation Externals
        public short ImageMaxSize { get; set; }
        public short VideoMaxSize { get; set; }
        private short ImageMaxCount { get; set; }
        private short VideoMaxCount { get; set; }
        private int ExistingImages { get; set; }
        private int ExistingVideos { get; set; }
        private bool MediaIsRequired { get; set; }
        public void SetValidationExternals(
            short imageMaxCount,
            short imageMaxSize,
            short videoMaxCount,
            short videoMaxSize,
            int existingImages,
            int existingVideos,
            bool mediaIsRequired
        )
        {
            ImageMaxCount = imageMaxCount;
            ImageMaxSize = imageMaxSize;
            VideoMaxCount = videoMaxCount;
            VideoMaxSize = videoMaxSize;
            ExistingImages = existingImages;
            ExistingVideos = existingVideos;
            MediaIsRequired = mediaIsRequired;
        }
        #endregion

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var field =
                !new[] {
                    ServiceEnum.Moving,
                    ServiceEnum.Removal,
                    ServiceEnum.Pickup,
                    ServiceEnum.Transport
                }.Contains(Service) ? AnnotationEnum.Request.Requirement.ServiceType :
                string.IsNullOrWhiteSpace(Title) ? AnnotationEnum.Request.Requirement.Title :
                string.IsNullOrWhiteSpace(PickupAddress) ? AnnotationEnum.Request.Requirement.PickupAddress :
                (Service != ServiceEnum.Removal && string.IsNullOrWhiteSpace(DeliveryAddress)) ? AnnotationEnum.Request.Requirement.DeliveryAddress :
                (Cost < 1 || Cost > 10000) ? AnnotationEnum.Request.Requirement.Cost :
                (!IsASAP && !DateOnly.TryParse(Date, out _)) ? AnnotationEnum.Request.Requirement.Date :
                (!IsASAP && !TimeOnly.TryParse(Time, out _)) ? AnnotationEnum.Request.Requirement.Time :
                (Service == ServiceEnum.Transport && string.IsNullOrWhiteSpace(VehicleType)) ? AnnotationEnum.Request.Requirement.VehicleType :
                (Service == ServiceEnum.Transport && string.IsNullOrWhiteSpace(VehicleCondition)) ? AnnotationEnum.Request.Requirement.VehicleCondition :
                null;

            if (field is not null)
            {
                yield return new ValidationResult(
                    string.Format(AnnotationEnum.Request.Requirement.Error, field)
                );
            }

            Images ??= [];
            Videos ??= [];
            KeepFileIds ??= [];

            var totalImages = Images.Length + ExistingImages;
            var totalVideos = Videos.Length + ExistingVideos;

            if (MediaIsRequired && totalImages == 0)
            {
                yield return new ValidationResult(AnnotationEnum.Request.Media.ImageMinCount);
            }
            if (totalImages > ImageMaxCount)
            {
                yield return new ValidationResult(
                    string.Format(AnnotationEnum.Request.Media.ImageMaxCount, ImageMaxCount)
                );
            }
            if (totalVideos > VideoMaxCount)
            {
                yield return new ValidationResult(
                    string.Format(AnnotationEnum.Request.Media.VideoMaxCount, VideoMaxCount)
                );
            }

            Title = Title.Trim();
            Description = Description?.Trim();
            PickupAddress = PickupAddress?.Trim();
            PickupZipCode = PickupZipCode?.Trim();
            DeliveryAddress = DeliveryAddress?.Trim();
            DeliveryZipCode = DeliveryZipCode?.Trim();
            VehicleType = VehicleType?.Trim();
            VehicleCondition = VehicleCondition?.Trim();
        }
    }
}
