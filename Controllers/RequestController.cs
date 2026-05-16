using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
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
    IFileRepository fileRepository,
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
        return View("Form");
    }

    [HttpPost]
    public async Task<IActionResult> Create(RequestViewModel model)
    {
        // validate user
        var user = await ValidateUser();
        if (user is null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        // validate requirement
        var requirement = ValidateRequirement(model);
        if (requirement is not null)
        {
            return Json(new
            {
                success = false,
                error = requirement
            });
        }

        // validate settings
        var settings = await settingsRepository.Get();

        // validate media
        var media = ValidateMedia(model, settings, []);
        if (media is not null)
        {
            return Json(new
            {
                success = false,
                error = media
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
        await UploadMedia(model, settings, []);

        // all good
        return Json(new
        {
            success = true,
            redirect = Url.Action("Index", "Dashboard")
        });
    }

    #endregion

    #region Edit

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var user = await ValidateUser();
        var request = await ValidateRequest(id, user?.Id ?? 0);

        if (user is null || request is null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var settings = await settingsRepository.Get();
        var requestFiles = await requestFileRepository.Load(id);
        var files = await fileRepository.Load(new BaseFilter<long>
        {
            Ids = requestFiles.Select(rf => rf.FileId).ToList()
        });

        ViewBag.Settings = settings;
        ViewBag.Request = request;
        ViewBag.Files = files.Select(i => new
        {
            fileId = i.Id,
            url = Url.Action("Download", "File", new { key = i.Key }, Request.Scheme),
            fileName = i.FileName,
            size = i.Size,
            isMain = requestFiles.First(rf => rf.Id == i.Id).IsMain,
            type = requestFiles.First(rf => rf.Id == i.Id).Type
        });
        return View("Form");
    }

    [HttpPost]
    public async Task<IActionResult> Edit(RequestViewModel model)
    {
        var user = await ValidateUser();
        var request = await ValidateRequest(model.Id, user?.Id ?? 0);

        if (user is null || request is null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        // validate requirement
        var requirement = ValidateRequirement(model);
        if (requirement is not null)
        {
            return Json(new
            {
                success = false,
                error = requirement
            });
        }

        // ...
        var settings = await settingsRepository.Get();
        var existingFiles = await requestFileRepository.Load(model.Id);

        // validate media
        var media = ValidateMedia(model, settings, existingFiles);
        if (media is not null)
        {
            return Json(new
            {
                success = false,
                error = media
            });
        }

        // update request fields
        request.Service = model.Service;
        request.Title = model.Title.Trim();
        request.Description = model.Description?.Trim() ?? "";
        request.SourceAddress = model.SourceAddress.Trim();
        request.DestinationAddress = model.DestinationAddress.Trim();
        request.ProposedCost = model.ProposedCost;
        request.ProposedCurrency = "EUR";
        request.ProposedASAP = model.IsASAP;
        request.ProposedDate = !model.IsASAP ? DateOnly.Parse(model.ProposedDate!) : null;
        request.ProposedTime = !model.IsASAP ? TimeOnly.Parse(model.ProposedTime!) : null;
        await requestRepository.Update(request);

        // upload media
        await UploadMedia(model, settings, existingFiles);

        // all good
        return Json(new
        {
            success = true,
            redirect = Url.Action("Index", "Dashboard")
        });
    }

    #endregion

    private async Task<string?> UploadMedia(RequestViewModel request, SettingsEntity settings, List<RequestFileEntity> existingFiles)
    {
        request.Images ??= [];
        request.Videos ??= [];
        request.KeepFileIds ??= [];
        var fileEntities = new List<RequestFileEntity>();

        // seek existing files to be deleted
        var deletions = existingFiles.Where(i => !request.KeepFileIds.Contains(i.FileId)).ToList();

        // delete request files not being kept and their storage
        await requestFileRepository.Delete([.. deletions.Select(i => i.Id)]);
        foreach (var file in deletions)
        {
            await fileService.Delete(file.FileId);
        }

        // add new images ...
        for (var i = 0; i < request.Images.Length; i++)
        {
            var file = await fileService.Create(
                request.Images[i],
                null,
                settings.RequestImageMaxSize,
                [FileTypeEnum.PNG, FileTypeEnum.JPEG]
            );
            if (file.Error is not null)
            {
                return file.Error;
            }
            fileEntities.Add(new RequestFileEntity
            {
                RequestId = request.Id,
                FileId = file.Id ?? 0,
                Type = RequestFileTypeEnum.Image
            });
            if (i == request.MainImageIndex)
            {
                request.KeepMainFileId = file.Id ?? 0;
            }
        }

        // add new videos ...
        foreach (var vid in request.Videos)
        {
            var file = await fileService.Create(
                vid,
                null,
                settings.RequestVideoMaxSize,
                [FileTypeEnum.MP4, FileTypeEnum.MOV]
            );
            if (file.Error is not null)
            {
                return file.Error;
            }
            fileEntities.Add(new RequestFileEntity
            {
                RequestId = request.Id,
                FileId = file.Id ?? 0,
                Type = RequestFileTypeEnum.Video
            });
        }

        // save new request files
        await requestFileRepository.Create(fileEntities);

        // set main file
        await requestFileRepository.SetMainImage(request.Id, request.KeepMainFileId);

        // ...
        return null;
    }

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

    private async Task<RequestEntity?> ValidateRequest(long id, long userId)
    {
        var request = await requestRepository.Get(id);

        if (request is null || request.RequesterId != userId || request.Status != RequestStatusEnum.Pending)
        {
            return null;
        }

        return request;
    }

    private static string? ValidateRequirement(RequestViewModel model)
    {
        var result =
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
            (!model.IsASAP && !DateOnly.TryParse(model.ProposedDate, out var date)) ? "Proposed Date" :
            (!model.IsASAP && !TimeOnly.TryParse(model.ProposedTime, out var time)) ? "Proposed Time" :
            null;

        return result is null ? null : result + " field is required.";
    }

    private static string? ValidateMedia(RequestViewModel request, SettingsEntity settings, List<RequestFileEntity> existingFiles)
    {
        request.Images ??= [];
        request.Videos ??= [];
        request.KeepFileIds ??= [];

        var totalImages = request.Images.Length +
                          existingFiles.Count(i =>
                            i.Type == RequestFileTypeEnum.Image &&
                            request.KeepFileIds.Contains(i.FileId)
                          );
        var totalVideos = request.Videos.Length +
                          existingFiles.Count(i =>
                            i.Type == RequestFileTypeEnum.Video &&
                            request.KeepFileIds.Contains(i.FileId)
                          );

        if (totalImages == 0)
        {
            return "Image field is required.";
        }
        if (totalImages > settings.RequestImageMaxCount)
        {
            return $"Maximum {settings.RequestImageMaxCount} images allowed.";
        }
        if (totalVideos > settings.RequestVideoMaxCount)
        {
            return $"Maximum {settings.RequestVideoMaxCount} videos allowed.";
        }

        return null;
    }

}
