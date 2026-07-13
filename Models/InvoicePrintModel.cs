using Bewegdeal.Data.Entities;

namespace Bewegdeal.Models
{
    public class InvoicePrintModel
    {
        public InvoiceEntity? Data { get; set; }
        public UserEntity? Company { get; set; }
    }
}
