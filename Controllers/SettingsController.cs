using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers;

[RequireAdmin]
public class SettingsController(
    ISettingsRepository settingsRepository,
    IFileRepository fileRepository,
    FileService fileService) : Controller
{

    #region Index

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await settingsRepository.Get();

        ViewBag.TermsFile = await fileRepository.Get(settings.TermsAndConditionsFileId);

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

        var (id, error) = await fileService.Create(termsFile, settings.TermsAndConditionsFileId, null, [FileTypeEnum.PDF]);
        if (error is not null || id is null)
        {
            TempData["TermsError"] = error;
            return RedirectToAction(nameof(Index));
        }

        settings.TermsAndConditionsFileId = id.Value;
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
