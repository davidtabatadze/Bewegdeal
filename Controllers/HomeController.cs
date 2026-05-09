using Bewegdeal.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult Users() => View();
        public IActionResult Settings() => View();
    }
}
