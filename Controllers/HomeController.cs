using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class HomeController(IUserRepository userRepository) : Controller
    {
        public async Task<IActionResult> Index()
        {
            if (long.TryParse(HttpContext.Session.GetString("UserId"), out var userId))
            {
                var user = await userRepository.Get(new UserFilter { Id = userId });
                if (user is not null && !user.AcquaintedHIW && user.Role != UserRoleEnum.Administrator)
                {
                    var action = user.Role == UserRoleEnum.Customer ? "Customer" : "Company";
                    return RedirectToAction(action, "HowItWorks");
                }
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
