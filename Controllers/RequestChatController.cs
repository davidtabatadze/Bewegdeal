using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers;

[Authorize]
public class RequestChatController(RequestChatService RequestChatService) : XBaseController
{

    [HttpGet]
    public async Task<IActionResult> Visibility(string requestNumber)
    {
        return Json(new
        {
            mode = await RequestChatService.GetMode(requestNumber, UserId, UserRole)
        });
    }

    [HttpPost]
    [Authorize(Roles = UserRoleEnum.Company)]
    public async Task<IActionResult> Initiate(string requestNumber)
    {
        return Json(await RequestChatService.Initiate(requestNumber, UserId));
    }

    [HttpGet]
    public async Task<IActionResult> Conversation(string requestNumber)
    {
        var conversation = await RequestChatService.Conversation(requestNumber, UserId);
        if (conversation is null)
        {
            return Content("");
        }
        return PartialView("Conversation", conversation);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(string requestNumber)
    {
        await RequestChatService.Cancel(requestNumber, UserId);
        return Json(GenericResultModel.Ok());
    }

}