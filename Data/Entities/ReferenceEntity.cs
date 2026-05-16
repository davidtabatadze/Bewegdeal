using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class ReferenceEntity : IEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
