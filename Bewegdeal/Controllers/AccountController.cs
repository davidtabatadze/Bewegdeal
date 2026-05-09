using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Bewegdeal.Controllers;

public class AccountController(IUserRepository userRepository, IMemoryCache cache) : Controller
{

    #region Login

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("UserId") is not null)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe)
    {
        // seek user
        var user = await userRepository.Get(new UserFilter { Email = email });

        // verify user existence and password
        if (user is null || !PasswordTool.Verify(password, user.Password, user.Salt))
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        // verify user status
        switch (user.Status)
        {

            case UserStatusEnum.Blocked:
                ViewBag.Error = "Your account has been blocked. Please, contact support.";
                return View();

            case UserStatusEnum.Pending:
                ViewBag.Error = "Your account is pending approval. Please, wait for confirmation.";
                return View();

            case UserStatusEnum.Unverified:
                return RedirectToAction(nameof(VerifyEmail), new { email = user.Email });

        }

        // fill up the session
        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("UserRole", user.Role);
        HttpContext.Session.SetString("UserName", user.Name);

        // set persistent cookie when "remember me" is checked
        if (rememberMe)
        {
            Response.Cookies.Append("bewegdeal_remember", user.Id.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            });
        }

        // all good
        return RedirectToAction("Index", "Home");
    }

    #endregion

    [HttpPost]
    public IActionResult Logout()
    {
        // clear session
        HttpContext.Session.Clear();

        // remove the remember-me cookie
        Response.Cookies.Delete("bewegdeal_remember");

        return RedirectToAction("Index", "Landing");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

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
        email = (email ?? "").Trim();
        ViewBag.Email = email;

        // seek one time code
        var oneTimeCodeCacheKey = CacheKeyTool.Get(CacheKeyEnum.VerificationEmail, email);
        var oneTimeCode = cache.Get<string>(oneTimeCodeCacheKey);
        cache.Remove(oneTimeCodeCacheKey);

        // no code? error
        if (oneTimeCode is null)
        {
            ViewBag.Error = "Verification code has expired. Please, request a new one.";
            return View();
        }

        // wrong input? error
        if (oneTimeCode != otp)
        {
            ViewBag.Error = "Invalid verification code. Please, reenter or request a new one.";
            return View();
        }

        // update user
        var user = await userRepository.Get(new UserFilter { Email = email });
        if (user is not null)
        {
            user.Status = user.Role == UserRoleEnum.Customer ?
                          UserStatusEnum.Active : UserStatusEnum.Pending;
            await userRepository.Update(user);
        }

        // all good
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
        if (!string.IsNullOrWhiteSpace(mailError))
        {
            ViewBag.Error = mailError;
            return View();
        }

        // all good        
        ViewBag.Success = "A new verification code has been sent to your email.";
        return View("VerifyEmail");
    }

    #endregion

    #region Register

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

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
        var existing = await userRepository.Get(new UserFilter { Email = model.Email });
        if (existing is not null)
        {
            ViewBag.Error = "An account with this email address already exists.";
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

        // ready password
        var (hash, salt) = PasswordTool.HashPassword(model.Password);

        // do create user
        var user = await userRepository.Create(new UserEntity
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
            Status = UserStatusEnum.Unverified
        });

        // send verification email
        var mailError = await SendVerificationEmail(user.Email);
        if (!string.IsNullOrWhiteSpace(mailError))
        {
            ViewBag.Error = mailError;
            return View(model);
        }

        // all done
        return RedirectToAction(nameof(VerifyEmail), new { email = user.Email });
    }

    #endregion

    /// <summary>
    /// Sends a verification email containing a one-time code to the specified email address.
    /// </summary>
    /// <remarks>The verification code is valid for 10 minutes. If the email cannot be sent, the returned
    /// error message can be displayed to the user or logged for troubleshooting.</remarks>
    /// <param name="email">The email address to which the verification code will be sent. Cannot be null or empty.</param>
    /// <returns>An empty string if the email was sent successfully; otherwise, an error message describing the failure.</returns>
    private async Task<string> SendVerificationEmail(string email)
    {
        // generate one-time code
        var oneTimeCode = Random.Shared.Next(100000, 1000000).ToString();

        // cache the code for later verification
        cache.Set(
            CacheKeyTool.Get(CacheKeyEnum.VerificationEmail, email),
            oneTimeCode,
            TimeSpan.FromMinutes(10)
        );

        // send email
        var result = await BrevoTool.Send(
            email,
            "Verify your Bewegdeal account",
            $"""
            <p>Hello,</p>
            <p>Your Bewegdeal verification code is:</p>
            <p style="font-size:28px;font-weight:bold;letter-spacing:6px">{oneTimeCode}</p>
            <p>This code expires in <strong>10 minutes</strong>.</p>
            <p>If you did not register on Bewegdeal, please ignore this email.</p>
            """
        );

        // all good
        if (result == EmailStatusEnum.Sent)
        {
            return "";
        }

        // something went wrong
        return "We are sorry, we are unable to send you a verification email right now. Please, try again later or contact the site administration.";
    }

}
