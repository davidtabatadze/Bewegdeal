using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class HomeController(IUserRepository userRepository) : Controller
    {
        public IActionResult Index() => View();

        public async Task<IActionResult> Users()
        {
            var users = await userRepository.GetAll(new UserFilter());
            ViewBag.TotalCount = users.Count;
            ViewBag.CustomerCount = users.Count(u => u.Role == UserRoleEnum.Customer);
            ViewBag.CompanyCount = users.Count(u => u.Role == UserRoleEnum.Company);
            ViewBag.PendingCount = users.Count(u => u.Status == UserStatusEnum.Pending);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await userRepository.GetAll(new UserFilter());
            var data = users.Select(u => new
            {
                id = u.Id,
                name = u.Name,
                email = u.Email,
                mobile = u.Mobile,
                role = u.Role,
                status = u.Status
            });
            return Json(new { data });
        }

        public IActionResult Settings() => View();
    }
}
