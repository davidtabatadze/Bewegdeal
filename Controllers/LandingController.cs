using Bewegdeal.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString(ConstantEnum.SessionUserId) is not null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}
