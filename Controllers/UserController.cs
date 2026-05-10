using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Bewegdeal.Models;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class UserController(IUserRepository userRepository) : Controller
    {
        public async Task<IActionResult> List()
        {
            var users = await userRepository.Load(new UserFilter() { Id = 0 });
            ViewBag.TotalCount = users.Count;
            ViewBag.CustomerCount = users.Count(u => u.Role == UserRoleEnum.Customer);
            ViewBag.CompanyCount = users.Count(u => u.Role == UserRoleEnum.Company);
            ViewBag.PendingCount = users.Count(u => u.Status == UserStatusEnum.Pending);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LoadUsers([FromQuery] UserFilter filter, [FromQuery] int draw = 1)
        {
            var users = await userRepository.Load(filter);
            var filtered = await userRepository.Count(filter);
            var total = await userRepository.Count(new UserFilter());

            var data = users.Select(u => new
            {
                id = u.Id,
                name = u.Name,
                email = u.Email,
                mobile = u.Mobile,
                address = u.Address,
                role = u.Role,
                status = u.Status,
                interests = u.Interests
            });

            return Json(new DataTablesResult<object>(draw, total, filtered, data));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserStatus(long id)
        {
            if (id.ToString() == HttpContext.Session.GetString("UserId"))
            {
                return BadRequest();
            }

            var user = await userRepository.Get(new UserFilter { Id = id });

            if (user is null)
            {
                return NotFound();
            }

            var newStatus = user.Status switch
            {
                UserStatusEnum.Active => UserStatusEnum.Blocked,
                UserStatusEnum.Blocked => UserStatusEnum.Active,
                UserStatusEnum.Pending => UserStatusEnum.Active,
                _ => user.Status
            };

            await userRepository.SetUserStatus(id, newStatus);
            return Json(new { status = newStatus });
        }
    }
}
