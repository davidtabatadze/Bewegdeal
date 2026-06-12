using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Services;
using Bewegdeal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers;

[Authorize]
public class RequestController(RequestService RequestService) : XBaseController
{
    #region List

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var data = await RequestService.LoadGrid(UserId);

        ViewBag.ViewerRole = data.Result!.viewerRole;
        ViewBag.ViewerInterests = data.Result!.viewerInterests;
        ViewBag.CustomerHasRequests = data.Result!.customerHasRequests;
        ViewBag.TotalCount = 0; // await requestRepository.Count(new RequestFilter { ViewerId = viewerId, ViewerRole = viewerRole, ViewerInterests = user?.Interests ?? [] });
        ViewBag.PendingCount = 0; // await requestRepository.Count(new RequestFilter { ViewerId = viewerId, ViewerRole = viewerRole, Status = RequestStatusEnum.Pending });
        ViewBag.NegotiationCount = 0; // await requestRepository.Count(new RequestFilter { ViewerId = viewerId, ViewerRole = viewerRole, Status = RequestStatusEnum.Negotiation });
        ViewBag.ResolvedCount = 0; // await requestRepository.Count(new RequestFilter { ViewerId = viewerId, ViewerRole = viewerRole, Status = RequestStatusEnum.Resolved });
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> LoadRequests([FromQuery] RequestFilter filter, [FromQuery] int draw = 1)
    {
        return Json(await RequestService.LoadGrid(filter, draw, UserId, BaseUrl));
    }

    #endregion

    #region Create

    [HttpGet]
    [Authorize(Roles = UserRoleEnum.Customer)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Request = await RequestService.Get();
        return View("Form");
    }

    [HttpPost]
    [Authorize(Roles = UserRoleEnum.Customer)]
    public async Task<IActionResult> Create(RequestViewModel model)
    {
        // validate
        await RequestService.PrepareValidation(model);
        ModelState.Clear();
        TryValidateModel(model);
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

        // create request
        var request = await RequestService.Create(UserId, model);
        if (!request.Success)
        {
            return Json(new
            {
                success = false,
                error = request.Message
            });
        }

        // all good
        return Json(new
        {
            success = true,
            redirect = Url.Action("View", "Request", new { number = request.Result?.Number ?? "-" })
        });
    }

    #endregion

    #region Edit

    [HttpGet]
    [Authorize(Roles = UserRoleEnum.Customer)]
    public async Task<IActionResult> Edit(long id)
    {
        var result = await RequestService.Get(id, UserId);

        if (!result.Success || result.Result is null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Request = result.Result;
        return View("Form");
    }

    [HttpPost]
    [Authorize(Roles = UserRoleEnum.Customer)]
    public async Task<IActionResult> Edit(RequestViewModel model)
    {
        // validate
        await RequestService.PrepareValidation(model);
        ModelState.Clear();
        TryValidateModel(model);
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

        // update request
        var request = await RequestService.Update(UserId, model);
        if (!request.Success)
        {
            return Json(new
            {
                success = false,
                error = request.Message
            });
        }

        // all good
        return Json(new
        {
            success = true,
            redirect = Url.Action("View", "Request", new { number = request.Result?.Number ?? "-" })
        });
    }

    #endregion

    #region View

    [HttpGet]
    [ActionName("View")]
    public async Task<IActionResult> ViewRequest(string number)
    {
        var result = await RequestService.Get(number, UserId);

        if (!result.Success || result.Result is null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Request = result.Result;
        return View("View");
    }

    #endregion
}
