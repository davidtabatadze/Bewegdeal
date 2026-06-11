using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [Authorize]
    public class ChatController(ChatService ChatService) : XBaseController
    {
        [Authorize(Roles = UserRoleEnum.Administrator)]
        public IActionResult List()
        {
            return View();
        }

        [Authorize(Roles = UserRoleEnum.Administrator)]
        [HttpGet]
        public async Task<IActionResult> LoadChats([FromQuery] ChatFilter filter, [FromQuery] int draw = 1)
        {
            return Json(await ChatService.LoadGrid(filter, draw));
        }
    }
}
