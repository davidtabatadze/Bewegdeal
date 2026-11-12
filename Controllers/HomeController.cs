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

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? code)
        {
            ViewBag.Code = code ?? (Response.StatusCode >= 400 ? Response.StatusCode : 500);
            return View();
        }
    }
}
