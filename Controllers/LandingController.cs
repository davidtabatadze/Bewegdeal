using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class LandingController : XBaseController
    {
        public IActionResult Index()
        {
            if (UserId is not null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}
