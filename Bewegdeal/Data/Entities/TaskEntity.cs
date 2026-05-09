using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class TaskEntity : IEntity
    {
        public long     Id              { get; set; }
        public long     UserId          { get; set; }
        public string   Type            { get; set; } = "";
        public string   Name            { get; set; } = "";
        public string?  Description     { get; set; }
        public string?  Image           { get; set; }
        public string?  Media           { get; set; }
        public decimal? Cost            { get; set; }
        public string?  Currency        { get; set; }
        public string?  PickupAddress   { get; set; }
        public string?  DeliveryAddress { get; set; }
        public string   Status          { get; set; } = "";
        public int      Views           { get; set; }
        public DateTime CreatedAt       { get; set; }
    }
}
