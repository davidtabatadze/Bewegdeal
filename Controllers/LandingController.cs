using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index() => View();
    }
}
