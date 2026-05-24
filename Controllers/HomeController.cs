using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class HomeController(FileService fileService, IUserRepository userRepository) : XBaseController(fileService, userRepository)
    {
        public async Task<IActionResult> Index()
        {
            var user = await GetUser();
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
