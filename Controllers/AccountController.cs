using Bewegdeal.Enums;
using Bewegdeal.Services;
using Bewegdeal.Tools;
using Bewegdeal.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace Bewegdeal.Controllers;

public class AccountController(AccountService AccountService, FileService FileService) : XBaseController
{

    #region Login

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity!.IsAuthenticated)
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
            return RedirectToAction(nameof(VerifyAccount), new
            {
                email = result.Object?.Email ?? "-",
                mobile = result.Object?.Mobile ?? "-"
            });
        }
        else if (result.Message is not null)
        {
            ViewBag.Error = result.Message;
            return View();
        }

        var user = result.Object!;

        var pictureUrl = await FileService.GetUrl(user.AvatarFileId);
        var principal = UserIdentityTool.BuildPrincipal(user, pictureUrl);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
            }
        );

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
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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

    #region Verify Account

    [HttpGet]
    public IActionResult VerifyAccount(string email, string mobile)
    {
        ViewBag.Email = email;
        ViewBag.Mobile = mobile;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifyAccount(string email, string mobile, string emailOtp, string mobileOtp)
    {
        ViewBag.Email = email;
        ViewBag.Mobile = mobile;

        var result = await AccountService.VerifyAccount(email, mobile, emailOtp, mobileOtp);

        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            return View();
        }

        TempData["LoginSuccess"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> VerifyResend(string email, string mobile)
    {
        ViewBag.Email = email;
        ViewBag.Mobile = mobile;

        var result = await AccountService.VerifySend(email, mobile);

        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            return View("VerifyAccount");
        }

        ViewBag.Success = result.Message;
        return View("VerifyAccount");
    }

    #endregion

    #region Register

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegistrationViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegistrationViewModel model)
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

        return RedirectToAction(nameof(VerifyAccount), new { email = model.Email, mobile = model.Mobile });
    }

    #endregion

}
