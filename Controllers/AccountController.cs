using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Services;
using Bewegdeal.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Bewegdeal.Controllers;

public class AccountController(
    FileService fileService,
    IUserRepository userRepository,
    ISettingsRepository settingsRepository,
    IFileRepository fileRepository,
    IMemoryCache cache) : XBaseController(userRepository)
{
    private readonly IUserRepository _userRepository = userRepository;

    #region Login

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString(ConstantEnum.SessionUserId) is not null)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe)
    {
        // seek user
        var user = await GetUser(email);

        // verify user existence and password
        if (user is null || !PasswordTool.Verify(password, user.Password, user.Salt))
        {
            ViewBag.Error = AnnotationEnum.Account.Login.Credentials;
            return View();
        }

        // verify user status
        switch (user.Status)
        {

            case UserStatusEnum.Blocked:
                ViewBag.Error = AnnotationEnum.Account.Login.Blocked;
                return View();

            case UserStatusEnum.Pending:
                ViewBag.Error = AnnotationEnum.Account.Login.Pending;
                return View();

            case UserStatusEnum.Unverified:
                return RedirectToAction(nameof(VerifyEmail), new { email = user.Email });

        }

        // fill up the session
        HttpContext.Session.SetString(ConstantEnum.SessionUserId, user.Id.ToString());
        HttpContext.Session.SetString(ConstantEnum.SessionUserRole, user.Role);
        HttpContext.Session.SetString(ConstantEnum.SessionUserName, user.Name);
        HttpContext.Session.SetString(ConstantEnum.SessionUserEmail, user.Email);
        HttpContext.Session.SetString(ConstantEnum.SessionUserTheme, user.Theme);

        // set persistent cookie when "remember me" is checked
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

        // redirect to hiw
        if (!user.AcquaintedHIW && user.Role != UserRoleEnum.Administrator)
        {
            var action = user.Role == UserRoleEnum.Customer ? "Customer" : "Company";
            return RedirectToAction(action, "HowItWorks");
        }

        // all good
        return RedirectToAction("Index", "Home");
    }

    #endregion

    #region Logout

    [HttpPost]
    public IActionResult Logout()
    {
        // clear session
        HttpContext.Session.Clear();

        // remove the remember-me cookie
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
        var user = await GetUser(email);

        if (user is not null)
        {
            // send verification email
            var mailError = await SendResetEmail(user.Email, user.Name);
            if (mailError is not null)
            {
                TempData["ForgotError"] = mailError;
                return RedirectToAction(nameof(ForgotPassword));
            }
        }

        // always show success — never reveal whether an email exists
        TempData["ForgotSuccess"] = AnnotationEnum.Account.ForgotPassword.Success;
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
        token = token ?? "-";
        ViewBag.Token = token;

        // load cache
        var tokenKey = CacheKeyTool.Get(CacheKeyEnum.PasswordReset, token);
        var email = cache.Get<string>(tokenKey) ?? "-";
        var emailKey = CacheKeyTool.Get(CacheKeyEnum.PasswordReset, email);
        var lastToken = cache.Get<string>(emailKey) ?? "-";

        // clear cache
        cache.Remove(tokenKey);
        cache.Remove(emailKey);

        // load user
        var user = await GetUser(email);

        // validate
        if (user is null || lastToken != token)
        {
            TempData["ForgotError"] = AnnotationEnum.Account.ResetPassword.Expired;
            return RedirectToAction(nameof(ForgotPassword));
        }

        // update password and clear token
        var (hash, salt) = PasswordTool.HashPassword(password);
        await _userRepository.UpdatePassword(user.Id, hash, salt);

        TempData["LoginSuccess"] = AnnotationEnum.Account.ResetPassword.Success;
        return RedirectToAction(nameof(Login));
    }

    #endregion

    #region Verify Email

    [HttpGet]
    public IActionResult VerifyEmail(string email)
    {
        email = (email ?? "").Trim();
        ViewBag.Email = email;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifyEmail(string email, string otp)
    {
        // ready email
        ViewBag.Email = email;

        // seek one time code
        var oneTimeCodeCacheKey = CacheKeyTool.Get(CacheKeyEnum.EmailVerification, email);
        var oneTimeCode = cache.Get<string>(oneTimeCodeCacheKey);

        // no code? error
        if (oneTimeCode is null)
        {
            ViewBag.Error = AnnotationEnum.Account.VerifyEmail.Expired;
            return View();
        }

        // wrong input? error
        if (oneTimeCode != otp)
        {
            ViewBag.Error = AnnotationEnum.Account.VerifyEmail.Invalid;
            return View();
        }

        // update user
        var user = await GetUser(email);
        if (user is not null)
        {
            await _userRepository.SetUserStatus(
                user.Id,
                user.Role == UserRoleEnum.Customer ?
                UserStatusEnum.Active : UserStatusEnum.Pending
            );
        }

        // all good
        cache.Remove(oneTimeCodeCacheKey);
        TempData["LoginSuccess"] = AnnotationEnum.Account.VerifyEmail.Success;
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> VerifyResend(string email)
    {
        // ready email
        email = (email ?? "").Trim();
        ViewBag.Email = email;

        // send verification email
        var mailError = await SendVerificationEmail(email);
        if (mailError is not null)
        {
            ViewBag.Error = mailError;
            return View("VerifyEmail");
        }

        // all good
        ViewBag.Success = AnnotationEnum.Account.VerifyEmail.Resent;
        return View("VerifyEmail");
    }

    #endregion

    #region Register

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        var settings = await settingsRepository.Get();
        var file = await fileRepository.Get(settings.TermsAndConditionsFileId);
        ViewBag.TermsFileKey = file?.Key;

        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // validate input
        if (!ModelState.IsValid)
        {
            ViewBag.Error = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault();
            return View(model);
        }

        // validate email uniqueness
        var existing = await GetUser(model.Email);
        if (existing is not null)
        {
            ViewBag.Error = AnnotationEnum.Account.Register.Exists;
            return View(model);
        }

        // setup interests
        var interests = model.Role == UserRoleEnum.Customer ?
                        [] :
                        new string[] {
                            model.ServiceMoving ?? "",
                            model.ServiceJunk ?? "",
                            model.ServiceStorePickup ?? "",
                            model.ServiceVehicle ?? ""
                        }.Where(i => !string.IsNullOrWhiteSpace(i));

        // ready terms of service
        long? termsFileId = null;
        if (model.Role == UserRoleEnum.Company && model.TermsFile is not null)
        {
            var file = await fileService.Create(model.TermsFile, null, 5, [FileTypeEnum.PDF]);
            if (file.Error is not null)
            {
                ViewBag.Error = file.Error;
                return View(model);
            }
            termsFileId = file.Id;
        }

        // ready password
        var (hash, salt) = PasswordTool.HashPassword(model.Password);

        // do create user
        var user = await _userRepository.Create(new UserEntity
        {
            Role = model.Role,
            Name = model.Name,
            Email = model.Email,
            Number = model.Number,
            Mobile = model.Mobile,
            Address = model.Address,
            Password = hash,
            Salt = salt,
            Interests = [.. interests],
            Status = UserStatusEnum.Unverified,
            ServiceTermsFileId = termsFileId,
            AcquaintedHIW = false,
            Theme = model.Theme == UserThemeEnum.Dark ? UserThemeEnum.Dark : UserThemeEnum.Light
        });

        // send verification email
        var mailError = await SendVerificationEmail(user.Email);
        if (mailError is not null)
        {
            ViewBag.Error = mailError;
            return View(model);
        }

        // all done
        return RedirectToAction(nameof(VerifyEmail), new { email = user.Email });
    }

    #endregion

    private async Task<string?> SendVerificationEmail(string email)
    {
        // generate one-time code
        var oneTimeCode = Random.Shared.Next(100000, 1000000).ToString();

        // cache the code for later verification
        cache.Set(
            CacheKeyTool.Get(CacheKeyEnum.EmailVerification, email),
            oneTimeCode,
            TimeSpan.FromMinutes(
                Convert.ToInt64(ConstantEnum.EmailVerificationTimeout)
            )
        );

        // send email
        var result = await BrevoTool.Send(
            email,
            "Verify your Bewegdeal account",
            $"""
            <p>Hello,</p>
            <p>Your Bewegdeal verification code is:</p>
            <p style="font-size:28px;font-weight:bold;letter-spacing:6px">{oneTimeCode}</p>
            <p>This code expires in <strong>{ConstantEnum.EmailVerificationTimeout} minutes</strong>.</p>
            <p>If you did not register on Bewegdeal, please ignore this email.</p>
            """
        );

        // all good
        if (result == EmailStatusEnum.Sent)
        {
            return null;
        }

        // something went wrong
        return AnnotationEnum.Account.Email.Verification;
    }

    private async Task<string?> SendResetEmail(string email, string name)
    {
        // generate a token
        var tokenBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes).ToLower();

        // cache 
        cache.Set(
            CacheKeyTool.Get(CacheKeyEnum.PasswordReset, token),
            email,
            TimeSpan.FromMinutes(ConstantEnum.ResetPasswordTimeout)
        );
        cache.Set(
            CacheKeyTool.Get(CacheKeyEnum.PasswordReset, email),
            token,
            TimeSpan.FromMinutes(ConstantEnum.ResetPasswordTimeout)
        );

        // build reset link
        var resetLink = Url.Action(nameof(ResetPassword), "Account", new { token }, Request.Scheme);

        // send email
        var result = await BrevoTool.Send(
            email,
            "Reset your Bewegdeal password",
            $"""
            <p>Hello {name},</p>
            <p>We received a request to reset the password for your Bewegdeal account.</p>
            <p><a href="{resetLink}" style="font-size:16px;font-weight:bold">Reset Password</a></p>
            <p>This link expires in <strong>{ConstantEnum.ResetPasswordTimeout} minutes</strong>.</p>
            <p>If you did not request a password reset, you can safely ignore this email.</p>
            """
        );

        // all good
        if (result == EmailStatusEnum.Sent)
        {
            return null;
        }

        // something went wrong
        return AnnotationEnum.Account.Email.Reset;
    }

}
