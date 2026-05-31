using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class LandingController : XBaseController
    {
        public IActionResult Index()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}
