using Bewegdeal.Enums;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers;

[Authorize(Roles = UserRoleEnum.Administrator)]
public class SettingsController(SettingService SettingService) : XBaseController
{

    #region Index

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await SettingService.Get());
    }

    #endregion

    #region Save About Us

    [HttpPost]
    public async Task<IActionResult> SaveAboutUs(string? content)
    {
        await SettingService.SaveAboutUs(content);

        TempData["OpenSection"] = "aboutUs";
        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Save Privacy Policy

    [HttpPost]
    public async Task<IActionResult> SavePrivacyPolicy(string? content)
    {
        await SettingService.SavePrivacyPolicy(content);

        TempData["OpenSection"] = "privacyPolicy";
        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Save Term And Condition

    [HttpPost]
    public async Task<IActionResult> SaveTermsAndConditionsCustomer(string? content)
    {
        await SettingService.SaveTermsAndConditionsCustomer(content);

        TempData["OpenSection"] = "tcCustomer";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SaveTermsAndConditionsCompany(string? content)
    {
        await SettingService.SaveTermsAndConditionsCompany(content);

        TempData["OpenSection"] = "tcCompany";
        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Save Mobile 

    [HttpPost]
    public async Task<IActionResult> SaveMobile(string? mobilePrefix)
    {
        await SettingService.SaveMobile(mobilePrefix);

        TempData["OpenSection"] = "mobile";
        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Save Invoice

    [HttpPost]
    public async Task<IActionResult> SaveInvoice(short commissionPersent, short taxPersent, short dueDays)
    {
        if (commissionPersent <= 0 || taxPersent <= 0 || dueDays <= 0)
        {
            TempData["OpenSection"] = "invoice";
            return RedirectToAction(nameof(Index));
        }

        await SettingService.SaveInvoice(commissionPersent, taxPersent, dueDays);

        TempData["OpenSection"] = "invoice";
        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Save Request

    [HttpPost]
    public async Task<IActionResult> SaveRequest(short imageMaxCount, short imageMaxSize, short videoMaxCount, short videoMaxSize)
    {
        if (imageMaxCount <= 0 || imageMaxSize <= 0 || videoMaxCount <= 0 || videoMaxSize <= 0)
        {
            TempData["OpenSection"] = "request";
            return RedirectToAction(nameof(Index));
        }

        await SettingService.SaveRequest(imageMaxCount, imageMaxSize, videoMaxCount, videoMaxSize);

        TempData["OpenSection"] = "request";
        return RedirectToAction(nameof(Index));
    }

    #endregion

}
