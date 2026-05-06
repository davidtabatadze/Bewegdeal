using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers;

public class AuthenticationController : Controller
{
    public IActionResult Login() => View();

    public IActionResult ForgotPassword() => View();

    public IActionResult VerifyEmail() => View();

    public IActionResult Register() => View();
}
