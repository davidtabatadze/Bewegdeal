using Bewegdeal.Enums;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [Authorize]
    public class DashboardController(DashboardService DashboardService) : XBaseController
    {
        public IActionResult Index()
        {
            if (User.IsInRole(UserRoleEnum.Administrator))
            {
                return View("Admin");
            }
            if (User.IsInRole(UserRoleEnum.Company))
            {
                return View("Company");
            }
            if (User.IsInRole(UserRoleEnum.Customer))
            {
                return RedirectToAction("List", "Request");
            }
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [Authorize(Roles = UserRoleEnum.Company)]
        public async Task<IActionResult> CompanyStats(short year = 0)
        {
            var result = await DashboardService.GetDataForCompany(UserId, year);
            return Json(result);
        }

        [HttpGet]
        [Authorize(Roles = UserRoleEnum.Company)]
        public async Task<IActionResult> CompanyStats2(short year = 0)
        {
            var result = await DashboardService.GetDataForCompany2(UserId, year);
            return Json(result);
        }
    }
}
