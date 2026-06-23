using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers;

[Authorize(Roles = UserRoleEnum.Administrator)]
public class SettingsController(ISettingsRepository settingsRepository) : XBaseController
{

    #region Index

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await settingsRepository.Get());
    }

    #endregion

    #region Save Term And Condition Settings

    [HttpPost]
    public async Task<IActionResult> SaveTermAndConditionSettings(string? termsContent)
    {
        var settings = await settingsRepository.Get();
        settings.TermsAndConditionsContent = termsContent ?? string.Empty;
        settings.TermsAndConditionsContentDate = DateTime.Now;
        await settingsRepository.Update(settings);

        TempData["TermsCustomerSuccess"] = "Customer Terms & Conditions updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SaveTermAndConditionSettingsCompany(string? termsContent)
    {
        var settings = await settingsRepository.Get();
        settings.TermsAndConditionsContentCompany = termsContent ?? string.Empty;
        settings.TermsAndConditionsContentDateCompany = DateTime.Now;
        await settingsRepository.Update(settings);

        TempData["TermsCompanySuccess"] = "Company Terms & Conditions updated successfully.";
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
