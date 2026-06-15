using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Services;
using Bewegdeal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers;

[Authorize]
public class RequestChatController(RequestChatService RequestChatService) : XBaseController
{

    [HttpGet]
    public async Task<IActionResult> Visibility(string requestNumber)
    {
        var data = await RequestChatService.GetMode(requestNumber, UserId, UserRole);
        return Json(data.mode != ChatModeEnum.None);
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
        return PartialView("~/Views/Chat/Conversation.cshtml", conversation);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(string requestNumber)
    {
        await RequestChatService.Cancel(requestNumber, UserId);
        return Json(GenericResultModel.Ok());
    }

    [HttpPost]
    [Authorize(Roles = UserRoleEnum.Company)]
    public async Task<IActionResult> Propose(RequestProposalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new
            {
                success = false,
                error = ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .FirstOrDefault()
            });
        }

        await RequestChatService.Propose(UserId, model);
        return Json(GenericResultModel.Ok());
    }

    [HttpPost]
    [Authorize(Roles = UserRoleEnum.Customer)]
    public async Task<IActionResult> ProposalReact(long id, bool accepted, string? reason = null)
    {
        await RequestChatService.ProposalReact(UserId, id, accepted, reason);
        return Json(GenericResultModel.Ok());
    }

    [HttpGet]
    public async Task<IActionResult> ProposalCard(long proposalId)
    {
        var model = await RequestChatService.GetProposal(proposalId);
        if (model is null)
        {
            return Content("");
        }
        return PartialView("~/Views/Proposal/_ProposalCard.cshtml", model);
    }

}