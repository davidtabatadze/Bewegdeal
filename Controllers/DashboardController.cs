using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class DashboardController : XBaseController
    {
        public IActionResult Index()
        {
            return UserRole switch
            {
                UserRoleEnum.Administrator => View("Admin"),
                UserRoleEnum.Company => View("Company"),
                UserRoleEnum.Customer => RedirectToAction("List", "Request"),
                _ => RedirectToAction("Login", "Account")
            };
        }
    }
}
