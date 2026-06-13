using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [Authorize]
    public class DashboardController(
        IUserRepository UserRepository,
        IRequestRepository RequestRepository,
        IChatRepository ChatRepository) : XBaseController
    {
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole(UserRoleEnum.Administrator))
            {
                await LoadAdminStats();
                return View("Admin");
            }
            if (User.IsInRole(UserRoleEnum.Company))
            {
                await LoadCompanyStats();
                return View("Company");
            }
            if (User.IsInRole(UserRoleEnum.Customer))
            {
                return RedirectToAction("List", "Request");
            }
            return RedirectToAction("Login", "Account");
        }

        private async Task LoadAdminStats()
        {
            ViewBag.TotalClients = await UserRepository.Count(new UserFilter { Role = UserRoleEnum.Customer });
            ViewBag.TotalCompanies = await UserRepository.Count(new UserFilter { Role = UserRoleEnum.Company });
            ViewBag.PendingCompanies = await UserRepository.Count(new UserFilter { Role = UserRoleEnum.Company, Status = UserStatusEnum.Pending });
            ViewBag.FraudChats = await ChatRepository.Count(new ChatFilter { Fraud = ChatFraudEnum.Dubious });

            var movingCount = await RequestRepository.Count(new RequestFilter { Service = ServiceEnum.Moving });
            var removalCount = await RequestRepository.Count(new RequestFilter { Service = ServiceEnum.Removal });
            var pickupCount = await RequestRepository.Count(new RequestFilter { Service = ServiceEnum.Pickup });
            var transportCount = await RequestRepository.Count(new RequestFilter { Service = ServiceEnum.Transport });
            var totalRequests = movingCount + removalCount + pickupCount + transportCount;

            ViewBag.TotalRequests = totalRequests;
            ViewBag.MovingCount = movingCount;
            ViewBag.RemovalCount = removalCount;
            ViewBag.PickupCount = pickupCount;
            ViewBag.TransportCount = transportCount;
            ViewBag.MovingPct = totalRequests > 0 ? (int)Math.Round((double)movingCount / totalRequests * 100) : 0;
            ViewBag.RemovalPct = totalRequests > 0 ? (int)Math.Round((double)removalCount / totalRequests * 100) : 0;
            ViewBag.PickupPct = totalRequests > 0 ? (int)Math.Round((double)pickupCount / totalRequests * 100) : 0;
            ViewBag.TransportPct = totalRequests > 0 ? (int)Math.Round((double)transportCount / totalRequests * 100) : 0;

            ViewBag.PendingRequests = await RequestRepository.Count(new RequestFilter { Status = RequestStatusEnum.Pending });
            ViewBag.NegotiationRequests = await RequestRepository.Count(new RequestFilter { Status = RequestStatusEnum.Negotiation });
            ViewBag.AgreedRequests = await RequestRepository.Count(new RequestFilter { Status = RequestStatusEnum.Agreed });
            ViewBag.ResolvedRequests = await RequestRepository.Count(new RequestFilter { Status = RequestStatusEnum.Resolved });
            ViewBag.CancelledRequests = await RequestRepository.Count(new RequestFilter { Status = RequestStatusEnum.Cancelled });
        }

        private async Task LoadCompanyStats()
        {
            var companyId = UserId;

            var user = await UserRepository.Get(new UserFilter { Id = companyId }, [nameof(UserEntity.Interests)]);
            var interests = user?.Interests ?? [];

            ViewBag.OngoingChats = await ChatRepository.Count(new ChatFilter { CompanyId = companyId, Status = ChatStatusEnum.Ongoing });
            ViewBag.AgreedDeals = await ChatRepository.Count(new ChatFilter { CompanyId = companyId, Status = ChatStatusEnum.Agreed });
            ViewBag.CancelledChats = await ChatRepository.Count(new ChatFilter { CompanyId = companyId, Status = ChatStatusEnum.Cancelled });
            ViewBag.TotalChats = await ChatRepository.Count(new ChatFilter { CompanyId = companyId });
            ViewBag.PotentialRequests = await RequestRepository.Count(new RequestFilter
            {
                ViewerRole = UserRoleEnum.Company,
                ViewerId = companyId,
                ViewerFocus = RequestViewerFocusEnum.Potential,
                ViewerInterests = interests,
            });
            ViewBag.MyRequests = await RequestRepository.Count(new RequestFilter
            {
                ViewerRole = UserRoleEnum.Company,
                ViewerId = companyId,
                ViewerFocus = RequestViewerFocusEnum.Mine,
                ViewerInterests = interests,
            });
        }
    }
}
