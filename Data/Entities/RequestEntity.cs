using Bewegdeal.Data.Base;
using Bewegdeal.Enums;

namespace Bewegdeal.Data.Entities
{
    public class RequestEntity : IEntity
    {
        public long Id { get; set; }
        public string Number { get; set; } = "";
        public DateTime CreateDate { get; set; }
        public string Status { get; set; } = RequestStatusEnum.Pending;
        public string Service { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string PickupAddress { get; set; } = "";
        public string DeliveryAddress { get; set; } = "";
        public long RequesterId { get; set; }
        public long? ExecutorId { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; } = "EUR";
        public bool ASAP { get; set; }
        public DateOnly? Date { get; set; }
        public TimeOnly? Time { get; set; }
        public long? AgreementId { get; set; }
        public string? VehicleType { get; set; }
        public string? VehicleCondition { get; set; }
        public bool PresentElevator { get; set; }
        public bool PresentParking { get; set; }
    }
}
