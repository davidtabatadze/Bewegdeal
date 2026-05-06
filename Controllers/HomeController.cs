using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult Users() => View();
        public IActionResult Settings() => View();
    }
}
