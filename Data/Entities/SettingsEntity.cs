using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class SettingsEntity : IEntity
    {
        public long Id { get; set; }
        public string TermsAndConditionsContent { get; set; } = string.Empty;
        public DateTime TermsAndConditionsContentDate { get; set; }
        public string TermsAndConditionsContentCompany { get; set; } = string.Empty;
        public DateTime TermsAndConditionsContentDateCompany { get; set; }
        public short RequestNegotiationMinutes { get; set; }
        public short RequestImageMaxCount { get; set; }
        public short RequestImageMaxSize { get; set; }
        public short RequestVideoMaxCount { get; set; }
        public short RequestVideoMaxSize { get; set; }
    }
}
