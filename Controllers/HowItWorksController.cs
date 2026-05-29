using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class HowItWorksController(UserService userService) : XBaseController
    {

        [HttpGet]
        public async Task<IActionResult> Customer()
        {
            var user = await userService.GetValidUser(UserId, roles: [UserRoleEnum.Customer]);
            if (user is null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.ShowBar = !user.AcquaintedHIW;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Company()
        {
            var user = await userService.GetValidUser(UserId, roles: [UserRoleEnum.Company]);
            if (user is null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.ShowBar = !user.AcquaintedHIW;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Acknowledge()
        {
            var user = await userService.GetValidUser(UserId);
            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            await userService.SetAcquaintedHIW(user.Id);
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
