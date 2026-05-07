using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    /// <summary>
    /// General lookup / reference data row (roles, statuses, etc.).
    /// The Id is a human-readable string key, e.g. "customer", "active".
    /// </summary>
    public class ReferenceEntity : IEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
