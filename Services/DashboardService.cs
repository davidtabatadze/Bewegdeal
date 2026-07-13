using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using System.Globalization;

namespace Bewegdeal.Services
{
    public class DashboardService(
        UserService UserService,
        RequestService RequestService,
        InvoiceService InvoiceService,
        ProposalService ProposalService,
        ChatService ChatService
    )
    {

        public async Task<GenericResultModel<object>> GetAdminBoardGeneral()
        {
            var pendingUsers = await UserService.Count(new UserFilter
            {
                Status = UserStatusEnum.Pending
            });
            var pendingInvoices = await InvoiceService.Count(new InvoiceFilter
            {
                Status = InvoiceStatusEnum.Pending
            });
            var pendingChats = await ChatService.Count(new ChatFilter
            {
                Fraud = ChatFraudEnum.Dubious
            });
            var servedCustomers = await InvoiceService.CountDistinct(new InvoiceFilter
            {
                Active = true
            }, nameof(InvoiceEntity.CustomerId));
            var profit = await InvoiceService.Sum(new InvoiceFilter
            {
                Status = InvoiceStatusEnum.Paid
            }, nameof(InvoiceEntity.TotalCost));

            return GenericResultModel<object>.Ok(new
            {
                pendingUsers,
                pendingInvoices,
                pendingChats,
                servedCustomers,
                profit
            });
        }

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

        public async Task<GenericResultModel<object>> GetBoardIncome(long userId, short year = 0)
        {
            var dateFilter = GetDateFilter(year);

            var invoices = await InvoiceService.Load(new InvoiceFilter
            {
                ViewerId = userId == 0 ? null : userId,
                ViewerRole = userId == 0 ? UserRoleEnum.Administrator : UserRoleEnum.Company,
                Active = userId == 0 ? null : true,
                Status = userId == 0 ? InvoiceStatusEnum.Paid : null,
                DateFrom = dateFilter.startDate,
                DateTo = dateFilter.endDate
            }, [nameof(InvoiceEntity.Id), nameof(InvoiceEntity.Service), nameof(InvoiceEntity.TotalCost), nameof(InvoiceEntity.ServiceCost), nameof(InvoiceEntity.CreateDate)]);

            var feesSum = new List<decimal>();
            var movingSum = new List<decimal>();
            var pickupSum = new List<decimal>();
            var removalSum = new List<decimal>();
            var transportSum = new List<decimal>();

            for (int month = 1; month <= 12; month++)
            {
                if (new DateTime(dateFilter.year, month, 1) > DateTime.Now)
                {
                    break;
                }

                var monthInvoices = invoices.Where(i => i.CreateDate.Month == month).ToList();

                feesSum.Add(monthInvoices.Sum(i => i.TotalCost));
                movingSum.Add(monthInvoices.Where(i => i.Service == ServiceEnum.Moving).Sum(i =>
                    userId == 0 ? i.TotalCost : i.ServiceCost - i.TotalCost
                ));
                pickupSum.Add(monthInvoices.Where(i => i.Service == ServiceEnum.Pickup).Sum(i =>
                    userId == 0 ? i.TotalCost : i.ServiceCost - i.TotalCost
                ));
                removalSum.Add(monthInvoices.Where(i => i.Service == ServiceEnum.Removal).Sum(i =>
                    userId == 0 ? i.TotalCost : i.ServiceCost - i.TotalCost
                ));
                transportSum.Add(monthInvoices.Where(i => i.Service == ServiceEnum.Transport).Sum(i =>
                    userId == 0 ? i.TotalCost : i.ServiceCost - i.TotalCost
                ));
            }

#pragma warning disable CS8604 // Possible null reference argument.
            return GenericResultModel<object>.Ok(new
            {
                dateFilter.year,
                dateFilter.years,
                chart = new
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

        public async Task<GenericResultModel<object>> GetBoardDeal(long userId, short year = 0)
        {
            var dateFilter = GetDateFilter(year);
            var startDate = DateOnly.FromDateTime(dateFilter.startDate);
            var endDate = DateOnly.FromDateTime(dateFilter.endDate > DateTime.Now ? DateTime.Now : dateFilter.endDate);

            var proposals = await ProposalService.Load(new RequestProposalFilter
            {
                CompanyId = userId == 0 ? null : userId,
                Status = RequestProposalStatusEnum.Accepted,
                DateFrom = startDate,
                DateTo = endDate
            }, [nameof(RequestProposalEntity.Date), nameof(RequestProposalEntity.Service)]);

            var movingCount = new List<decimal>();
            var pickupCount = new List<decimal>();
            var removalCount = new List<decimal>();
            var transportCount = new List<decimal>();

            for (int month = 1; month <= 12; month++)
            {
                if (new DateTime(dateFilter.year, month, 1) > DateTime.Now)
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
                dateFilter.year,
                dateFilter.years,
                chart = new
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

        public async Task<GenericResultModel<object>> GetBoardUser(short year = 0)
        {
            var dateFilter = GetDateFilter(year);

            var users = await UserService.Load(new UserFilter
            {
                ExcludeRole = UserRoleEnum.Administrator,
                DateFrom = dateFilter.startDate,
                DateTo = dateFilter.endDate
            }, [nameof(UserEntity.Role), nameof(UserEntity.CreateDate)]);

            var companyCount = new List<decimal>();
            var customerCount = new List<decimal>();

            for (int month = 1; month <= 12; month++)
            {
                if (new DateTime(dateFilter.year, month, 1) > DateTime.Now)
                {
                    break;
                }

                var monthUsers = users.Where(i => i.CreateDate.Month == month)
                                      .ToList();
                companyCount.Add(monthUsers.Count(i => i.Role == UserRoleEnum.Company));
                customerCount.Add(monthUsers.Count(i => i.Role == UserRoleEnum.Customer));
            }

#pragma warning disable CS8604 // Possible null reference argument.
            return GenericResultModel<object>.Ok(new
            {
                dateFilter.year,
                dateFilter.years,
                chart = new
                {
                    groupNames = new
                    {
                        company = AnnotationEnum.General.Role.Company,
                        customer = AnnotationEnum.General.Role.Customer
                    },
                    xaxisValues = DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames.Where(m => !string.IsNullOrEmpty(m)),
                    series = new List<object>
                    {
                        companyCount.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Role.Company, data = companyCount },
                        customerCount.All(v => v == 0) ? null : new { name = AnnotationEnum.General.Role.Customer, data = customerCount },
                    }.Where(s => s is not null)
                }
            });
#pragma warning restore CS8604 // Possible null reference argument.
        }

        public async Task<GenericResultModel<object>> GetBoardRequest()
        {
            var pending = await RequestService.Count(
                new RequestFilter { Status = RequestStatusEnum.Pending }
            );
            var negotiation = await RequestService.Count(
                new RequestFilter { Status = RequestStatusEnum.Negotiation }
            );
            var agreed = await RequestService.Count(
                new RequestFilter { Status = RequestStatusEnum.Agreed }
            );
            var resolved = await RequestService.Count(
                new RequestFilter { Status = RequestStatusEnum.Resolved }
            );

#pragma warning disable CS8604 // Possible null reference argument.
            return GenericResultModel<object>.Ok(new
            {
                chart = new
                {
                    groupNames = new
                    {
                        pending = pending > 0,
                        negotiation = negotiation > 0,
                        agreed = agreed > 0,
                        resolved = resolved > 0
                    },
                    labels = new string?[] {
                        pending > 0 ? AnnotationEnum.General.RequestStatus.Pending : null,
                        negotiation > 0 ? AnnotationEnum.General.RequestStatus.Negotiation : null,
                        agreed > 0 ? AnnotationEnum.General.RequestStatus.Agreed : null,
                        resolved > 0 ? AnnotationEnum.General.RequestStatus.Resolved : null
                    }.Where(s => s is not null),
                    series = new int?[] {
                        pending > 0 ? pending : null,
                        negotiation > 0 ? negotiation : null,
                        agreed > 0 ? agreed : null,
                        resolved > 0 ? resolved : null
                    }.Where(s => s is not null)
                }
            });
#pragma warning restore CS8604 // Possible null reference argument.
        }

        private (int year, int[] years, DateTime startDate, DateTime endDate) GetDateFilter(short year = 0)
        {
            short minYear = 2026; //2025;
            short maxYear = (short)DateTime.Now.Year;
            year = year < minYear || year > maxYear ? maxYear : year;

            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);

            return (year, Enumerable.Range(minYear, maxYear - minYear + 1).ToArray(), startDate, endDate);
        }

    }
}
