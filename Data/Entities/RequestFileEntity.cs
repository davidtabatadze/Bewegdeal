using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class RequestFileEntity : IEntity
    {
        public long Id { get; set; }
        public long RequestId { get; set; }
        public long FileId { get; set; }
        public bool IsMain { get; set; }
        public string Type { get; set; } = "";
    }
}
