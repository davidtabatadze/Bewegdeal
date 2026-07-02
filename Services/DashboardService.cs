using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using System.Globalization;

namespace Bewegdeal.Services
{
    public class DashboardService(InvoiceService InvoiceService)
    {

        public async Task<GenericResultModel<object>> GetDataForCompany(long userId, short year = 0)
        {
            short minYear = 2025;
            short maxYear = (short)DateTime.Now.Year;
            year = year < minYear || year > maxYear ? maxYear : year;

            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);

            var invoices = await InvoiceService.Load(new InvoiceFilter
            {
                ViewerId = userId,
                ViewerRole = UserRoleEnum.Company,
                Status = InvoiceStatusEnum.Paid,
                DateFrom = startDate,
                DateTo = endDate
            });

            var feesSums = new List<decimal>();
            var movingSums = new List<decimal>();
            var pickupSums = new List<decimal>();
            var removalSums = new List<decimal>();
            var transportSums = new List<decimal>();

            for (int month = 1; month <= 12; month++)
            {
                if (new DateTime(year, month, 1) > DateTime.Now)
                {
                    break;
                }

                var monthInvoices = invoices.Where(i => i.CreateDate.Month == month).ToList();
                feesSums.Add(monthInvoices.Sum(i => i.TotalCost));
                movingSums.Add(monthInvoices.Where(i => i.Service == ServiceEnum.Moving).Sum(i => i.ServiceCost - i.TotalCost));
                pickupSums.Add(monthInvoices.Where(i => i.Service == ServiceEnum.Pickup).Sum(i => i.ServiceCost - i.TotalCost));
                removalSums.Add(monthInvoices.Where(i => i.Service == ServiceEnum.Removal).Sum(i => i.ServiceCost - i.TotalCost));
                transportSums.Add(monthInvoices.Where(i => i.Service == ServiceEnum.Transport).Sum(i => i.ServiceCost - i.TotalCost));
            }

#pragma warning disable CS8604 // Possible null reference argument.
            return GenericResultModel<object>.Ok(new
            {
                year,
                years = Enumerable.Range(minYear, maxYear - minYear + 1).ToArray(),
                invoiceChart = new
                {
                    groupNames = new
                    {
                        moving = AnnotationEnum.General.Service.Moving,
                        pickup = AnnotationEnum.General.Service.Pickup,
                        removal = AnnotationEnum.General.Service.Removal,
                        transport = AnnotationEnum.General.Service.Transport,
                        fees = "Plattformgebühren"
                    },
                    xaxisValues = DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames.Where(m => !string.IsNullOrEmpty(m)),
                    series = new List<object>
                    {
                        movingSums.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Service.Moving, data = movingSums },
                        removalSums.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Service.Removal, data = removalSums },
                        pickupSums.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Service.Pickup, data = pickupSums },
                        transportSums.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Service.Transport, data = transportSums },
                        new { name = "Plattformgebühren", data = feesSums }
                    }.Where(s => s is not null)
                }
            });
#pragma warning restore CS8604 // Possible null reference argument.
        }

    }
}
