using Bewegdeal.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [Authorize]
    public class HowItWorksController : XBaseController
    {

        [HttpGet]
        [Authorize(Roles = UserRoleEnum.Customer)]
        public IActionResult Customer()
        {
            ViewBag.ShowBar = !HasClaim(IdentityFieldEnum.AcquaintedHIW, true);
            return View();
        }

        [HttpGet]
        [Authorize(Roles = UserRoleEnum.Company)]
        public IActionResult Company()
        {
            ViewBag.ShowBar = !HasClaim(IdentityFieldEnum.AcquaintedHIW, true);
            return View();
        }

    }
}
