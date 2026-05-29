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
    FileService fileService) : XBaseController(userRepository)
{
    #region List

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var user = await GetUser();
        var viewerId = user?.Id ?? 0;
        var viewerRole = user?.Role ?? "-";

        ViewBag.ViewerRole = viewerRole;
        ViewBag.ViewerInterests = user?.Interests ?? [];

        if (viewerRole == UserRoleEnum.Customer)
        {
            ViewBag.CustomerHasRequests = await requestRepository.Count(new RequestFilter { ViewerId = viewerId, ViewerRole = viewerRole }) > 0;
        }
        else
        {
            ViewBag.TotalCount = 0; // await requestRepository.Count(new RequestFilter { ViewerId = viewerId, ViewerRole = viewerRole, ViewerInterests = user?.Interests ?? [] });
            ViewBag.PendingCount = 0; // await requestRepository.Count(new RequestFilter { ViewerId = viewerId, ViewerRole = viewerRole, Status = RequestStatusEnum.Pending });
            ViewBag.NegotiationCount = 0; // await requestRepository.Count(new RequestFilter { ViewerId = viewerId, ViewerRole = viewerRole, Status = RequestStatusEnum.Negotiation });
            ViewBag.ResolvedCount = 0; // await requestRepository.Count(new RequestFilter { ViewerId = viewerId, ViewerRole = viewerRole, Status = RequestStatusEnum.Resolved });
        }
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> LoadRequests([FromQuery] RequestFilter filter, [FromQuery] int draw = 1)
    {
        var user = await GetUser();
        filter.ViewerId = user?.Id ?? 0;
        filter.ViewerRole = user?.Role ?? "-";
        filter.ViewerInterests = user?.Interests ?? [];

        var requests = await requestRepository.Load(filter);
        var filtered = await requestRepository.Count(filter);
        var total = await requestRepository.Count(new RequestFilter
        {
            ViewerId = user?.Id ?? 0,
            ViewerRole = user?.Role ?? "-",
            ViewerInterests = user?.Interests ?? []
        });

        var files = await requestFileRepository.LoadMainImages(
            requests.Count == 0 ? [0] : [.. requests.Select(r => r.Id)]
        );
        var images = await fileService.Load(new BaseFilter<long>
        {
            Ids = files.Count == 0 ? [0] : [.. files.Select(f => f.FileId)]
        });

        var data = requests.Select(r =>
        {
            var image = images.FirstOrDefault(i =>
                i.Id == files.FirstOrDefault(f => f.RequestId == r.Id)?.FileId
            );

            return (object)new
            {
                id = r.Id,
                number = r.Number,
                status = r.Status,
                service = r.Service,
                title = r.Title,
                createDate = r.CreateDate.ToString("MMM d, yyyy"),
                cost = r.Cost,
                currency = r.Currency,
                asap = r.ASAP,
                date = r.Date?.ToString("MMM d, yyyy"),
                time = r.Time?.ToString("HH:mm"),
                imageUrl = fileService.GetFileUrl(image, BaseUrl)
            };
        });

        return Json(new GridResultViewModel<object>(draw, total, filtered, data));
    }

    #endregion

    #region Create

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await GetUser(roles: [UserRoleEnum.Customer], active: true);
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
        var user = await GetUser(roles: [UserRoleEnum.Customer], active: true);
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
        var request = await requestRepository.Create(
            BuildRequest(null, model, user.Id)
        );

        // upload media
        model.Id = request.Id;
        var upload = await UploadMedia(model, settings, []);
        if (upload is not null)
        {
            return Json(new
            {
                success = false,
                error = upload
            });
        }

        // all good
        return Json(new
        {
            success = true,
            redirect = Url.Action("View", "Request", new { number = request.Number })
        });
    }

    #endregion

    #region View

    [HttpGet]
    [ActionName("View")]
    public async Task<IActionResult> ViewRequest(string number)
    {
        var request = await requestRepository.Get(number);
        if (request is null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var requestFiles = await requestFileRepository.Load(request.Id);
        var files = await fileService.Load(new BaseFilter<long>
        {
            Ids = [.. requestFiles.Select(rf => rf.FileId)]
        });

        var requester = await UserRepository.Get(new UserFilter { Id = request.RequesterId });

        var requesterNameParts = (requester?.Name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var requesterInitials = string.Concat(requesterNameParts.Take(2).Select(p => char.ToUpper(p[0])));
        if (string.IsNullOrEmpty(requesterInitials)) { requesterInitials = "?"; }

        ViewBag.Request = request;
        ViewBag.RequesterName = requester?.Name ?? "-";
        ViewBag.RequesterPictureUrl = await fileService.GetFileUrl(requester?.ProfilePictureFileId);

        ViewBag.RequesterInitials = requesterInitials;
        ViewBag.Files = files.Select(f => new
        {
            url = fileService.GetFileUrl(f, BaseUrl),
            fileName = f.FileName,
            isMain = requestFiles.First(rf => rf.FileId == f.Id).IsMain,
            type = requestFiles.First(rf => rf.FileId == f.Id).Type
        }).OrderBy(f => f.type).ThenByDescending(f => f.isMain);
        return View("View");
    }

    #endregion

    #region Edit

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var user = await GetUser(roles: [UserRoleEnum.Customer], active: true);
        var request = await ValidateRequest(id, user?.Id ?? 0);

        if (user is null || request is null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var settings = await settingsRepository.Get();
        var requestFiles = await requestFileRepository.Load(id);
        var files = await fileService.Load(new BaseFilter<long>
        {
            Ids = requestFiles.Select(rf => rf.FileId).ToList()
        });

        ViewBag.Settings = settings;
        ViewBag.Request = request;
        ViewBag.Files = files.Select(i => new
        {
            fileId = i.Id,
            url = fileService.GetFileUrl(i, BaseUrl),
            fileName = i.FileName,
            size = i.Size,
            isMain = requestFiles.First(rf => rf.FileId == i.Id).IsMain,
            type = requestFiles.First(rf => rf.FileId == i.Id).Type
        });
        return View("Form");
    }

    [HttpPost]
    public async Task<IActionResult> Edit(RequestViewModel model)
    {
        var user = await GetUser(roles: [UserRoleEnum.Customer], active: true);
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
        await requestRepository.Update(BuildRequest(request, model, user.Id));

        // upload media
        var upload = await UploadMedia(model, settings, existingFiles);
        if (upload is not null)
        {
            return Json(new
            {
                success = false,
                error = upload
            });
        }

        // all good
        return Json(new
        {
            success = true,
            redirect = Url.Action("View", "Request", new { number = request.Number })
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

    private async Task<RequestEntity?> ValidateRequest(long id, long userId)
    {
        var request = await requestRepository.Get(id);

        if (request is null || request.RequesterId != userId || request.Status != RequestStatusEnum.Pending)
        {
            return null;
        }

        return request;
    }

    private static RequestEntity BuildRequest(RequestEntity? entity, RequestViewModel request, long userId)
    {
        entity ??= new RequestEntity
        {
            Number = Guid.NewGuid().ToString("N"),
            CreateDate = DateTime.UtcNow,
            Status = RequestStatusEnum.Pending,
            RequesterId = userId
        };

        entity.Service = request.Service;
        entity.Title = request.Title.Trim();
        entity.Description = request.Description?.Trim() ?? "";
        entity.PickupAddress = request.PickupAddress.Trim();
        entity.DeliveryAddress = request.DeliveryAddress?.Trim() ?? "";
        entity.Cost = request.Cost;
        entity.Currency = "EUR";
        entity.ASAP = request.IsASAP;
        entity.Date = !request.IsASAP ? DateOnly.Parse(request.Date!) : null;
        entity.Time = !request.IsASAP ? TimeOnly.Parse(request.Time!) : null;

        return entity;
    }

    private static string? ValidateRequirement(RequestViewModel model)
    {
        var field =
            !new[] {
                ServiceEnum.Moving,
                ServiceEnum.Removal,
                ServiceEnum.Pickup,
                ServiceEnum.Transport
            }.Contains(model.Service) ? AnnotationEnum.Request.Requirement.ServiceType :
            string.IsNullOrWhiteSpace(model.Title) ? AnnotationEnum.Request.Requirement.Title :
            string.IsNullOrWhiteSpace(model.PickupAddress) ? AnnotationEnum.Request.Requirement.PickupAddress :
            (model.Service != ServiceEnum.Removal && string.IsNullOrWhiteSpace(model.DeliveryAddress)) ? AnnotationEnum.Request.Requirement.DeliveryAddress :
            (model.Cost < 1 || model.Cost > 10000) ? AnnotationEnum.Request.Requirement.Cost :
            (!model.IsASAP && !DateOnly.TryParse(model.Date, out _)) ? AnnotationEnum.Request.Requirement.Date :
            (!model.IsASAP && !TimeOnly.TryParse(model.Time, out _)) ? AnnotationEnum.Request.Requirement.Time :
            null;

        return field is null ? null : string.Format(AnnotationEnum.Request.Requirement.Error, field);
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
            return AnnotationEnum.Request.Media.ImageMinCount;
        }
        if (totalImages > settings.RequestImageMaxCount)
        {
            return string.Format(AnnotationEnum.Request.Media.ImageMaxCount, settings.RequestImageMaxCount);
        }
        if (totalVideos > settings.RequestVideoMaxCount)
        {
            return string.Format(AnnotationEnum.Request.Media.VideoMaxCount, settings.RequestVideoMaxCount);
        }

        return null;
    }

}
