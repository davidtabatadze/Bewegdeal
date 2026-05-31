using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace Bewegdeal.Controllers;

public class AccountController(AccountService AccountService, SettingService SettingService) : XBaseController
{

    #region Login

    [HttpGet]
    public IActionResult Login()
    {
        if (UserId.HasValue)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe)
    {
        var result = await AccountService.Login(email, password);

        if (result.Message == AnnotationEnum.Account.Login.Unverified)
        {
            return RedirectToAction(nameof(VerifyEmail), new { email });
        }
        else if (result.Message is not null)
        {
            ViewBag.Error = result.Message;
            return View();
        }

        var user = result.Object!;

        HttpContext.Session.SetString(ConstantEnum.SessionUserId, user.Id.ToString());
        HttpContext.Session.SetString(ConstantEnum.SessionUserRole, user.Role);
        HttpContext.Session.SetString(ConstantEnum.SessionUserName, user.Name);
        HttpContext.Session.SetString(ConstantEnum.SessionUserEmail, user.Email);
        HttpContext.Session.SetString(ConstantEnum.SessionUserTheme, user.Theme);
        if (user.ProfilePictureFileId.HasValue)
        {
            HttpContext.Session.SetString(
                ConstantEnum.SessionUserPictureId,
                user.ProfilePictureFileId.Value.ToString()
            );
        }

        if (rememberMe)
        {
            Response.Cookies.Append(ConstantEnum.CookieRemember, user.Id.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            });
        }

        if (!user.AcquaintedHIW && user.Role != UserRoleEnum.Administrator)
        {
            var action = user.Role == UserRoleEnum.Customer ? "Customer" : "Company";
            return RedirectToAction(action, "HowItWorks");
        }

        return RedirectToAction("Index", "Home");
    }

    #endregion

    #region Logout

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        Response.Cookies.Delete(ConstantEnum.CookieRemember);
        return RedirectToAction("Index", "Landing");
    }

    #endregion

    #region Forgot Password

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();
        var resetLink = Url.Action(nameof(ResetPassword), "Account", new { token }, Request.Scheme);

        var result = await AccountService.ForgotPassword(email, token, resetLink!);

        if (!result.Success)
        {
            TempData["ForgotError"] = result.Message;
            return RedirectToAction(nameof(ForgotPassword));
        }

        TempData["ForgotSuccess"] = result.Message;
        return RedirectToAction(nameof(ForgotPassword));
    }

    #endregion

    #region Reset Password

    [HttpGet]
    public IActionResult ResetPassword(string token)
    {
        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string token, string password)
    {
        ViewBag.Token = token;

        var result = await AccountService.ResetPassword(token, password);

        if (!result.Success)
        {
            TempData["ForgotError"] = result.Message;
            return RedirectToAction(nameof(ForgotPassword));
        }

        TempData["LoginSuccess"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    #endregion

    #region Verify Email

    [HttpGet]
    public IActionResult VerifyEmail(string email)
    {
        ViewBag.Email = email;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifyEmail(string email, string otp)
    {
        ViewBag.Email = email;

        var result = await AccountService.VerifyEmail(email, otp);

        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            return View();
        }

        TempData["LoginSuccess"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> VerifyResend(string email)
    {
        ViewBag.Email = email;

        var result = await AccountService.VerifySend(email);

        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            return View("VerifyEmail");
        }

        ViewBag.Success = result.Message;
        return View("VerifyEmail");
    }

    #endregion

    #region Register

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        ViewBag.TermsFileUrl = await SettingService.GetTermsAndConditionsUrl();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Error = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault();
            return View(model);
        }

        var result = await AccountService.Register(model);

        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            return View(model);
        }

        return RedirectToAction(nameof(VerifyEmail), new { email = model.Email });
    }

    #endregion

}
