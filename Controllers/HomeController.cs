using Bewegdeal.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [Authorize]
    public class HomeController : XBaseController
    {
        public IActionResult Index()
        {
            if (!HasClaim(IdentityFieldEnum.AcquaintedHIW, true) && !User.IsInRole(UserRoleEnum.Administrator))
            {
                return RedirectToAction("C" + UserRole.Substring(1), "HowItWorks");
            }

            if (User.IsInRole(UserRoleEnum.Customer))
            {
                return RedirectToAction("List", "Request");
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
