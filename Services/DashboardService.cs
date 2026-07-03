using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using System.Globalization;

namespace Bewegdeal.Services
{
    public class DashboardService(UserService UserService, RequestService RequestService, InvoiceService InvoiceService, ProposalService ProposalService)
    {

        public async Task<GenericResultModel<object>> GetCompanyBoardGeneral(long userId)
        {
            var company = await UserService.Get(userId, [nameof(UserEntity.Interests), nameof(UserEntity.Rating)]);

            var potentialRequests = await RequestService.Count(new RequestFilter
            {
                ViewerId = userId,
                ViewerRole = UserRoleEnum.Company,
                ViewerFocus = RequestViewerFocusEnum.Potential,
                ViewerInterests = company?.Interests ?? []
            });

            var servedCustomers = await InvoiceService.CountDistinct(new InvoiceFilter
            {
                Active = true,
                ViewerId = userId,
                ViewerRole = UserRoleEnum.Company
            }, nameof(InvoiceEntity.CustomerId));

            var paymentAmount = await InvoiceService.Sum(new InvoiceFilter
            {
                ViewerId = userId,
                ViewerRole = UserRoleEnum.Company,
                Status = InvoiceStatusEnum.Pending
            }, nameof(InvoiceEntity.TotalCost));

            var totalGains = await InvoiceService.Sum(new InvoiceFilter
            {
                Active = true,
                ViewerId = userId,
                ViewerRole = UserRoleEnum.Company
            }, nameof(InvoiceEntity.ServiceCost));

            var totalFees = await InvoiceService.Sum(new InvoiceFilter
            {
                Active = true,
                ViewerId = userId,
                ViewerRole = UserRoleEnum.Company
            }, nameof(InvoiceEntity.TotalCost));

            var servedRequests = await ProposalService.Count(new RequestProposalFilter
            {
                CompanyId = userId,
                Status = RequestProposalStatusEnum.Accepted,
                DateTo = DateOnly.FromDateTime(DateTime.Now)
            });

            return GenericResultModel<object>.Ok(new
            {
                rating = company?.Rating ?? 0,
                potentialRequests,
                paymentAmount,
                servedRequests,
                servedCustomers,
                profit = (long)(totalGains - totalFees)
            });
        }

        public async Task<GenericResultModel<object>> GetCompanyBoardIncome(long userId, short year = 0)
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
                Active = true,
                DateFrom = startDate,
                DateTo = endDate
            });

            var feesSum = new List<decimal>();
            var movingSum = new List<decimal>();
            var pickupSum = new List<decimal>();
            var removalSum = new List<decimal>();
            var transportSum = new List<decimal>();

            for (int month = 1; month <= 12; month++)
            {
                if (new DateTime(year, month, 1) > DateTime.Now)
                {
                    break;
                }

                var monthInvoices = invoices.Where(i => i.CreateDate.Month == month).ToList();
                feesSum.Add(monthInvoices.Sum(i => i.TotalCost));
                movingSum.Add(monthInvoices.Where(i => i.Service == ServiceEnum.Moving).Sum(i => i.ServiceCost - i.TotalCost));
                pickupSum.Add(monthInvoices.Where(i => i.Service == ServiceEnum.Pickup).Sum(i => i.ServiceCost - i.TotalCost));
                removalSum.Add(monthInvoices.Where(i => i.Service == ServiceEnum.Removal).Sum(i => i.ServiceCost - i.TotalCost));
                transportSum.Add(monthInvoices.Where(i => i.Service == ServiceEnum.Transport).Sum(i => i.ServiceCost - i.TotalCost));
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
                        movingSum.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Service.Moving, data = movingSum },
                        removalSum.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Service.Removal, data = removalSum },
                        pickupSum.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Service.Pickup, data = pickupSum },
                        transportSum.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Service.Transport, data = transportSum },
                        // new { name = "Plattformgebühren", data = feesSum }
                    }.Where(s => s is not null)
                }
            });
#pragma warning restore CS8604 // Possible null reference argument.
        }

        public async Task<GenericResultModel<object>> GetCompanyBoardDeal(long userId, short year = 0)
        {
            short minYear = 2025;
            short maxYear = (short)DateTime.Now.Year;
            year = year < minYear || year > maxYear ? maxYear : year;

            var startDate = DateOnly.FromDateTime(new DateTime(year, 1, 1));
            var endDate = DateOnly.FromDateTime(new DateTime(year, 12, 31));

            if (endDate > DateOnly.FromDateTime(DateTime.Now))
            {
                endDate = DateOnly.FromDateTime(DateTime.Now);
            }

            var proposals = await ProposalService.Load(new RequestProposalFilter
            {
                CompanyId = userId,
                Status = RequestProposalStatusEnum.Accepted,
                DateFrom = startDate,
                DateTo = endDate
            });

            var movingCount = new List<decimal>();
            var pickupCount = new List<decimal>();
            var removalCount = new List<decimal>();
            var transportCount = new List<decimal>();

            for (int month = 1; month <= 12; month++)
            {
                if (new DateTime(year, month, 1) > DateTime.Now)
                {
                    break;
                }

                var monthProposals = proposals.Where(i => i.Date is not null)
                                              .Where(i => i.Date!.Value.Month == month)
                                              .ToList();
                movingCount.Add(monthProposals.Count(i => i.Service == ServiceEnum.Moving));
                pickupCount.Add(monthProposals.Count(i => i.Service == ServiceEnum.Pickup));
                removalCount.Add(monthProposals.Count(i => i.Service == ServiceEnum.Removal));
                transportCount.Add(monthProposals.Count(i => i.Service == ServiceEnum.Transport));
            }

#pragma warning disable CS8604 // Possible null reference argument.
            return GenericResultModel<object>.Ok(new
            {
                year,
                years = Enumerable.Range(minYear, maxYear - minYear + 1).ToArray(),
                serviceChart = new
                {
                    groupNames = new
                    {
                        moving = AnnotationEnum.General.Service.Moving,
                        pickup = AnnotationEnum.General.Service.Pickup,
                        removal = AnnotationEnum.General.Service.Removal,
                        transport = AnnotationEnum.General.Service.Transport
                    },
                    xaxisValues = DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames.Where(m => !string.IsNullOrEmpty(m)),
                    series = new List<object>
                    {
                        movingCount.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Service.Moving, data = movingCount },
                        removalCount.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Service.Removal, data = removalCount },
                        pickupCount.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Service.Pickup, data = pickupCount },
                        transportCount.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Service.Transport, data = transportCount },
                    }.Where(s => s is not null)
                }
            });
#pragma warning restore CS8604 // Possible null reference argument.
        }

    }
}
