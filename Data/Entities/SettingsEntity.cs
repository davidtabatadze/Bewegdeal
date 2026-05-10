using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    /// <summary>
    /// Represents the single row in the Settings table.
    /// There is always exactly one row (Id = 1), created at startup by SettingsRepository.Seed().
    /// </summary>
    public class SettingsEntity : IEntity
    {
        public long Id { get; set; }
        public long TermsAndConditionsFileId { get; set; }
        public short RequestNegotiationMinutes { get; set; }
        public short RequestImageMaxCount { get; set; }
        public short RequestImageMaxSize { get; set; }
        public short RequestVideoMaxCount { get; set; }
        public short RequestVideoMaxSize { get; set; }
    }
}
