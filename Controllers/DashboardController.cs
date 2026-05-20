using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class DashboardController : Controller
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
    }
}
