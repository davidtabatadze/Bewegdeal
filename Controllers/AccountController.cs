using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Enums;
using Bewegdeal.Tools;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers;

public class AccountController(IUserRepository userRepository) : Controller
{
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = await userRepository.Get(new UserFilter { Email = email });

        if (user is null || !PasswordTool.Verify(password, user.Password, user.Salt))
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        switch (user.Status)
        {
            case UserStatusEnum.Blocked:
                ViewBag.Error = "Your account has been blocked. Please contact support.";
                return View();

            case UserStatusEnum.Pending:
                ViewBag.Error = "Your account is pending approval. Please wait for confirmation.";
                return View();

            case UserStatusEnum.Unverified:
                HttpContext.Session.SetString("PendingVerificationEmail", user.Email);
                return RedirectToAction(nameof(VerifyEmail));
        }

        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("UserRole", user.Role);
        HttpContext.Session.SetString("UserName", user.Name);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpGet]
    public IActionResult VerifyEmail() => View();

    [HttpGet]
    public IActionResult Register() => View();
}
