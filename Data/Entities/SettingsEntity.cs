using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class SettingsEntity : IEntity
    {
        public long Id { get; set; }
        public long TermsAndConditionsFileId { get; set; }
        public DateTime TermsAndConditionsFileDate { get; set; }
        public short RequestNegotiationMinutes { get; set; }
        public short RequestImageMaxCount { get; set; }
        public short RequestImageMaxSize { get; set; }
        public short RequestVideoMaxCount { get; set; }
        public short RequestVideoMaxSize { get; set; }
    }
}
