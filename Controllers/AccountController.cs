using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Enums;
using Bewegdeal.Models;
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
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // Reject invalid model before touching the database
        if (!ModelState.IsValid)
        {
            ViewBag.Error = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault();
            return View(model);
        }

        // Reject duplicate email addresses before creating anything
        var existing = await userRepository.Get(new UserFilter { Email = model.Email });
        if (existing is not null)
        {
            ViewBag.Error = "An account with this email address already exists.";
            return View(model);
        }

        // Collect selected service interests — Company only; Customer always gets an empty array
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

        // do create
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
            Status = UserStatusEnum.Unverified,
        });

        // Send the user to email verification — they cannot log in until the code is confirmed
        HttpContext.Session.SetString("PendingVerificationEmail", user.Email);
        return RedirectToAction(nameof(VerifyEmail));
    }
}
