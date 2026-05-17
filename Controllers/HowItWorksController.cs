using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class HowItWorksController(IUserRepository userRepository) : XBaseController(userRepository)
    {
        private readonly IUserRepository _userRepository = userRepository;

        [HttpGet]
        public async Task<IActionResult> Customer()
        {
            var user = await GetUser(roles: [UserRoleEnum.Customer]);
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
            var user = await GetUser(roles: [UserRoleEnum.Company]);
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
            var user = await GetUser();
            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            await _userRepository.SetAcquaintedHIW(user.Id);
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
