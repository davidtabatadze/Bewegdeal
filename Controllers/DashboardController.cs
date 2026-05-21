using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class DashboardController(
        IUserRepository userRepository,
        IRequestRepository requestRepository
    ) : XBaseController(userRepository)
    {
        public IActionResult Index()
        {
            return HttpContext.Session.GetString(ConstantEnum.SessionUserRole) switch
            {
                UserRoleEnum.Administrator => View("Admin"),
                UserRoleEnum.Company => View("Company"),
                UserRoleEnum.Customer => View("Customer"),
                _ => RedirectToAction("Login", "Account")
            };
        }

        [HttpGet]
        public async Task<IActionResult> CompanyStats(string? from, string? to)
        {
            var user = await GetUser(roles: [UserRoleEnum.Company], active: true);
            if (user is null) { return Json(new { error = "Unauthorized" }); }

            DateTime? dateFrom = null;
            DateTime? dateTo = null;
            if (DateTime.TryParse(from, out var f)) { dateFrom = f.Date; }
            if (DateTime.TryParse(to, out var t)) { dateTo = t.Date.AddDays(1).AddTicks(-1); }

            var requests = await requestRepository.Load(new RequestFilter
            {
                ExecutorId = user.Id,
                DateFrom = dateFrom,
                DateTo = dateTo
            });

            var completed = requests.Where(r => r.Status == RequestStatusEnum.Resolved).ToList();
            var rejected = requests.Where(r => r.Status == RequestStatusEnum.Cancelled).ToList();
            var inProgress = requests.Where(r =>
                r.Status == RequestStatusEnum.Negotiation ||
                r.Status == RequestStatusEnum.Agreed
            ).ToList();

            var ratingBase = completed.Count + rejected.Count;
            var rating = ratingBase > 0 ? Math.Round((decimal)completed.Count / ratingBase * 5, 1) : 0m;

            return Json(new
            {
                rating,
                ratingCount = ratingBase,
                completed = new
                {
                    total = completed.Count,
                    moving = completed.Count(r => r.Service == ServiceEnum.Moving),
                    removal = completed.Count(r => r.Service == ServiceEnum.Removal),
                    pickup = completed.Count(r => r.Service == ServiceEnum.Pickup),
                    transport = completed.Count(r => r.Service == ServiceEnum.Transport)
                },
                rejected = new
                {
                    total = rejected.Count,
                    moving = rejected.Count(r => r.Service == ServiceEnum.Moving),
                    removal = rejected.Count(r => r.Service == ServiceEnum.Removal),
                    pickup = rejected.Count(r => r.Service == ServiceEnum.Pickup),
                    transport = rejected.Count(r => r.Service == ServiceEnum.Transport)
                },
                revenue = new
                {
                    total = completed.Sum(r => r.Cost),
                    moving = completed.Where(r => r.Service == ServiceEnum.Moving).Sum(r => r.Cost),
                    removal = completed.Where(r => r.Service == ServiceEnum.Removal).Sum(r => r.Cost),
                    pickup = completed.Where(r => r.Service == ServiceEnum.Pickup).Sum(r => r.Cost),
                    transport = completed.Where(r => r.Service == ServiceEnum.Transport).Sum(r => r.Cost)
                },
                paidInvoices = new
                {
                    total = completed.Count,
                    moving = completed.Count(r => r.Service == ServiceEnum.Moving),
                    removal = completed.Count(r => r.Service == ServiceEnum.Removal),
                    pickup = completed.Count(r => r.Service == ServiceEnum.Pickup),
                    transport = completed.Count(r => r.Service == ServiceEnum.Transport)
                },
                pendingInvoices = new
                {
                    total = inProgress.Count,
                    moving = inProgress.Count(r => r.Service == ServiceEnum.Moving),
                    removal = inProgress.Count(r => r.Service == ServiceEnum.Removal),
                    pickup = inProgress.Count(r => r.Service == ServiceEnum.Pickup),
                    transport = inProgress.Count(r => r.Service == ServiceEnum.Transport)
                }
            });
        }
    }
}
