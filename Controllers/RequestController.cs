using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Bewegdeal.Models;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers;

[RequireLogin]
public class RequestController(
    IUserRepository userRepository,
    ISettingsRepository settingsRepository,
    IRequestRepository requestRepository,
    IRequestFileRepository requestFileRepository,
    FileService fileService) : Controller
{

    #region Create

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await ValidateUser();
        if (user is null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Settings = await settingsRepository.Get();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRequestViewModel model)
    {
        // validate user
        var user = await ValidateUser();
        if (user is null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        // validate fields
        var required =
            !new[] {
                ServiceEnum.Moving,
                ServiceEnum.Removal,
                ServiceEnum.Pickup,
                ServiceEnum.Transport
            }.Contains(model.Service) ? "Service Type" :
            string.IsNullOrWhiteSpace(model.Title) ? "Title" :
            string.IsNullOrWhiteSpace(model.SourceAddress) ? "Source Address" :
            string.IsNullOrWhiteSpace(model.DestinationAddress) ? "Destination Address" :
            (model.ProposedCost < 1 || model.ProposedCost > 10000) ? "Proposed Cost (1 to 10,000)" :
            (model.Images is null || model.Images.Length == 0) ? "Image" :
            (!model.IsASAP && !DateOnly.TryParse(model.ProposedDate, out var date)) ? "Proposed Date" :
            (!model.IsASAP && !TimeOnly.TryParse(model.ProposedTime, out var time)) ? "Proposed Time" :
            string.Empty;

        if (!string.IsNullOrWhiteSpace(required))
        {
            return Json(new
            {
                success = false,
                error = required + " field is required."
            });
        }

        // validate media
        var settings = await settingsRepository.Get();
        var mediaKind = (model.Images ?? []).Length > settings.RequestImageMaxCount ? RequestFileTypeEnum.Image :
                        (model.Videos ?? []).Length > settings.RequestVideoMaxCount ? RequestFileTypeEnum.Video : null;
        var mediaCount = mediaKind == RequestFileTypeEnum.Image ? settings.RequestImageMaxCount :
                         mediaKind == RequestFileTypeEnum.Video ? settings.RequestVideoMaxCount : 0;

        if (!string.IsNullOrWhiteSpace(mediaKind))
        {
            return Json(new
            {
                success = false,
                error = "Too many " + mediaKind + "s, maximum allowed count is " + mediaCount + "."
            });
        }

        // create request
        var request = await requestRepository.Create(new RequestEntity
        {
            Code = Guid.NewGuid(),
            Status = RequestStatusEnum.Pending,
            Service = model.Service,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim() ?? "",
            SourceAddress = model.SourceAddress.Trim(),
            DestinationAddress = model.DestinationAddress.Trim(),
            RequesterId = user.Id,
            ProposedCost = model.ProposedCost,
            ProposedCurrency = "EUR",
            ProposedASAP = model.IsASAP,
            ProposedDate = !model.IsASAP ? DateOnly.Parse(model.ProposedDate!) : null,
            ProposedTime = !model.IsASAP ? TimeOnly.Parse(model.ProposedTime!) : null,
        });

        // upload media
        model.Images = model.Images ?? [];
        model.Videos = model.Videos ?? [];
        var mainPicture = Math.Clamp(model.MainImageIndex, 0, model.Images.Length - 1);

        for (var i = 0; i < model.Images.Length; i++)
        {
            var file = await fileService.Create(
                model.Images[i],
                null,
                settings.RequestImageMaxSize,
                [FileTypeEnum.PNG, FileTypeEnum.JPEG]
            );
            if (file.Error is not null)
            {
                return Json(new { success = false, file.Error });
            }
            await requestFileRepository.Create(new RequestFileEntity
            {
                RequestId = request.Id,
                FileId = file.Id ?? 0,
                IsMain = i == mainPicture,
                Type = RequestFileTypeEnum.Image
            });
        }

        foreach (var vid in model.Videos)
        {
            var file = await fileService.Create(
                vid,
                null,
                settings.RequestVideoMaxSize,
                [FileTypeEnum.MP4, FileTypeEnum.MOV]
            );
            if (file.Error is not null)
            {
                return Json(new { success = false, file.Error });
            }
            await requestFileRepository.Create(new RequestFileEntity
            {
                RequestId = request.Id,
                FileId = file.Id ?? 0,
                IsMain = false,
                Type = RequestFileTypeEnum.Video
            });
        }

        return Json(new { success = true, redirect = Url.Action("Index", "Dashboard") });
    }

    #endregion

    private async Task<UserEntity?> ValidateUser()
    {
        if (!long.TryParse(HttpContext.Session.GetString("UserId"), out var userId))
        {
            return null;
        }

        var user = await userRepository.Get(new UserFilter { Id = userId });

        if (user is null || user.Role != UserRoleEnum.Customer || user.Status != UserStatusEnum.Active)
        {
            return null;
        }

        return user;
    }

}
