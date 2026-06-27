using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class SettingsEntity : IEntity
    {
        public long Id { get; set; }
        public string AboutUs { get; set; } = string.Empty;
        public string TermsAndConditionsContentCustomer { get; set; } = string.Empty;
        public string TermsAndConditionsContentCompany { get; set; } = string.Empty;
        public DateTime TermsAndConditionsContentDateCustomer { get; set; }
        public DateTime TermsAndConditionsContentDateCompany { get; set; }
        public short RequestNegotiationMinutes { get; set; }
        public short RequestImageMaxCount { get; set; }
        public short RequestImageMaxSize { get; set; }
        public short RequestVideoMaxCount { get; set; }
        public short RequestVideoMaxSize { get; set; }
        public short InvoiceCommissionPersent { get; set; }
        public short InvoiceTaxPersent { get; set; }
        public short InvoiceDueDays { get; set; }
        public string MobilePrefix { get; set; } = string.Empty;

    }
}
