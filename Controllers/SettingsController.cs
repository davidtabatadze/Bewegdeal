using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers;

[Authorize(Roles = UserRoleEnum.Administrator)]
public class SettingsController(ISettingsRepository settingsRepository, FileService fileService) : XBaseController
{

    #region Index

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await settingsRepository.Get();

        ViewBag.TermsFileUrl = await fileService.GetUrl(settings.TermsAndConditionsFileId);

        return View(settings);
    }

    #endregion

    #region Save Term And Condition Settings

    [HttpPost]
    public async Task<IActionResult> SaveTermAndConditionSettings(IFormFile? termsFile)
    {
        if (termsFile is null)
        {
            TempData["TermsError"] = "Please select a PDF file to upload.";
            return RedirectToAction(nameof(Index));
        }

        var settings = await settingsRepository.Get();

        var file = await fileService.Create(termsFile, settings.TermsAndConditionsFileId, null, [FileTypeEnum.PDF]);
        if (file.Message is not null || file.ObjectId is null)
        {
            TempData["TermsError"] = file.Message;
            return RedirectToAction(nameof(Index));
        }

        settings.TermsAndConditionsFileId = file.ObjectId.Value;
        await settingsRepository.Update(settings);

        TempData["TermsSuccess"] = "Terms & Conditions updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Save Request Settings

    [HttpPost]
    public async Task<IActionResult> SaveRequestSettings(
        short requestNegotiationMinutes,
        short requestImageMaxCount,
        short requestImageMaxSize,
        short requestVideoMaxCount,
        short requestVideoMaxSize)
    {
        if (requestNegotiationMinutes <= 0 ||
            requestImageMaxCount <= 0 ||
            requestImageMaxSize <= 0 ||
            requestVideoMaxCount <= 0 ||
            requestVideoMaxSize <= 0)
        {
            TempData["RequestError"] = "All request settings must be greater than zero.";
            return RedirectToAction(nameof(Index));
        }

        var settings = await settingsRepository.Get();

        settings.RequestNegotiationMinutes = requestNegotiationMinutes;
        settings.RequestImageMaxCount = requestImageMaxCount;
        settings.RequestImageMaxSize = requestImageMaxSize;
        settings.RequestVideoMaxCount = requestVideoMaxCount;
        settings.RequestVideoMaxSize = requestVideoMaxSize;

        await settingsRepository.Update(settings);

        TempData["RequestSuccess"] = "Request settings saved successfully.";
        return RedirectToAction(nameof(Index));
    }

    #endregion

}
