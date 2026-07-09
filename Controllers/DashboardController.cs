using Bewegdeal.Enums;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [Authorize]
    public class DashboardController(DashboardService DashboardService) : XBaseController
    {
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole(UserRoleEnum.Administrator))
            {
                var general = await DashboardService.GetAdminBoardGeneral();
                ViewBag.General = general.Result;
                return View("Admin");
            }
            if (User.IsInRole(UserRoleEnum.Company))
            {
                var general = await DashboardService.GetCompanyBoardGeneral(UserId);
                ViewBag.General = general.Result;
                return View("Company");
            }
            if (User.IsInRole(UserRoleEnum.Customer))
            {
                return RedirectToAction("List", "Request");
            }
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [Authorize(Roles = UserRoleEnum.Administrator)]
        public async Task<IActionResult> GetAdminBoardIncome(short year = 0)
        {
            var result = await DashboardService.GetBoardIncome(0, year);
            return Json(result);
        }

        [HttpGet]
        [Authorize(Roles = UserRoleEnum.Administrator)]
        public async Task<IActionResult> GetAdminBoardDeal(short year = 0)
        {
            var result = await DashboardService.GetBoardDeal(0, year);
            return Json(result);
        }

        [HttpGet]
        [Authorize(Roles = UserRoleEnum.Administrator)]
        public async Task<IActionResult> GetAdminBoardUser(short year = 0)
        {
            var result = await DashboardService.GetBoardUser(year);
            return Json(result);
        }

        [HttpGet]
        [Authorize(Roles = UserRoleEnum.Administrator)]
        public async Task<IActionResult> GetAdminBoardRequest()
        {
            var result = await DashboardService.GetBoardRequest();
            return Json(result);
        }

        [HttpGet]
        [Authorize(Roles = UserRoleEnum.Company)]
        public async Task<IActionResult> GetCompanyBoardIncome(short year = 0)
        {
            var result = await DashboardService.GetBoardIncome(UserId, year);
            return Json(result);
        }

        [HttpGet]
        [Authorize(Roles = UserRoleEnum.Company)]
        public async Task<IActionResult> GetCompanyBoardDeal(short year = 0)
        {
            var result = await DashboardService.GetBoardDeal(UserId, year);
            return Json(result);
        }
    }
}
