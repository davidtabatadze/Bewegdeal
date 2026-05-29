using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class HomeController(UserService userService) : XBaseController
    {
        public async Task<IActionResult> Index()
        {
            var user = await userService.GetValidUser(UserId);
            if (user is not null && !user.AcquaintedHIW && user.Role != UserRoleEnum.Administrator)
            {
                var action = user.Role == UserRoleEnum.Customer ? "Customer" : "Company";
                return RedirectToAction(action, "HowItWorks");
            }

            if (user?.Role == UserRoleEnum.Customer)
            {
                return RedirectToAction("List", "Request");
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
