using Bewegdeal.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [Authorize]
    public class DashboardController : XBaseController
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
    }
}
