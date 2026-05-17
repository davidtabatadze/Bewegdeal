using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class HowItWorksController(IUserRepository userRepository) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Customer()
        {
            var user = await GetUser();
            if (user is null || user.Role != UserRoleEnum.Customer)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.ShowBar = !user.AcquaintedHIW;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Company()
        {
            var user = await GetUser();
            if (user is null || user.Role != UserRoleEnum.Company)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.ShowBar = !user.AcquaintedHIW;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Acknowledge()
        {
            if (!long.TryParse(HttpContext.Session.GetString("UserId"), out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            await userRepository.SetAcquaintedHIW(userId);
            return RedirectToAction("Index", "Dashboard");
        }

        private async Task<UserEntity?> GetUser()
        {
            if (!long.TryParse(HttpContext.Session.GetString("UserId"), out var userId))
            {
                return null;
            }

            return await userRepository.Get(new UserFilter { Id = userId });
        }
    }
}
