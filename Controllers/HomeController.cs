using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
