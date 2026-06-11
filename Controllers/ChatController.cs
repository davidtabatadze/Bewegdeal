using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class ChatController : XBaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
