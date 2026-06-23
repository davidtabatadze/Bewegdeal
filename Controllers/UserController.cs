using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Services;
using Bewegdeal.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class UserController(UserService UserService) : XBaseController
    {

        #region List

        [Authorize(Roles = UserRoleEnum.Administrator)]
        public async Task<IActionResult> List()
        {
            ViewBag.TotalCount = 0;
            ViewBag.CustomerCount = 0;
            ViewBag.CompanyCount = 0;
            ViewBag.PendingCount = 0;
            return View();
        }

        [Authorize(Roles = UserRoleEnum.Administrator)]
        [HttpGet]
        public async Task<IActionResult> LoadUsers([FromQuery] UserFilter filter, [FromQuery] int draw = 1)
        {
            return Json(await UserService.LoadGrid(filter, draw));
        }

        [Authorize(Roles = UserRoleEnum.Administrator)]
        [HttpPost]
        public async Task<IActionResult> UpdateUserStatus(long id, string status)
        {
            var user = await UserService.Get(id, [nameof(UserEntity.Id), nameof(UserEntity.Status)]);

            if (user is null || UserId == user.Id || user.Status != status)
            {
                return BadRequest();
            }

            var newStatus = user.Status switch
            {
                UserStatusEnum.Active => UserStatusEnum.Blocked,
                UserStatusEnum.Blocked => UserStatusEnum.Active,
                UserStatusEnum.Pending => UserStatusEnum.Active,
                _ => user.Status
            };

            await UserService.Update(UserUpdateAreaEnum.Status, new UserEntity
            {
                Id = user.Id,
                Status = newStatus
            });

            return Json(new { status = newStatus });
        }

        [Authorize(Roles = UserRoleEnum.Administrator)]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(long id)
        {
            var user = await UserService.Get(id, [nameof(UserEntity.Id), nameof(UserEntity.Role)]);

            if (user is null || UserId == user.Id || user.Role == UserRoleEnum.Administrator)
            {
                return BadRequest();
            }

            await UserService.Delete(id);
            return Ok();
        }

        #endregion

        #region Profile

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var result = await UserService.GetProfile(UserId);

            if (result is null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Profile = result;
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateAvatar(IFormFile? avatar)
        {
            var result = await UserService.UpdateAvatar(UserId, avatar);

            if (!result.Success)
            {
                return BadRequest(new { result.Message });
            }

            await RefreshClaim(IdentityFieldEnum.AvatarUrl, result.Message);
            return Ok();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateTheme(string theme)
        {
            await UserService.Update(UserUpdateAreaEnum.Theme, new UserEntity
            {
                Id = UserId,
                Theme = theme
            });
            await RefreshClaim(IdentityFieldEnum.Theme, theme);

            return Ok();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ProfileError"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();
            }
            else
            {
                var result = await UserService.UpdateProfile(UserId, model);

                if (result.Success)
                {
                    await RefreshClaim(IdentityFieldEnum.Name, model?.Name ?? "?");
                }
                else
                {
                    if (result.Message is null)
                    {

                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        TempData["ProfileError"] = result.Message;
                    }
                }
            }

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdatePassword(string? newPassword, string? confirmPassword)
        {
            var result = await UserService.UpdatePassword(UserId, newPassword, confirmPassword);

            if (!result.Success)
            {
                TempData["PasswordError"] = result.Message;
                return RedirectToAction(nameof(Profile));
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        #endregion

        #region HIW

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AcceptHIW()
        {
            await UserService.Update(UserUpdateAreaEnum.AcceptHIW, new UserEntity
            {
                Id = UserId
            });
            await RefreshClaim(IdentityFieldEnum.AcquaintedHIW, true);
            return RedirectToAction("Index", "Dashboard");
        }

        #endregion

        #region Terms

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptTerms()
        {
            await UserService.Update(UserUpdateAreaEnum.AcceptTerms, new UserEntity
            {
                Id = UserId
            });
            await RefreshClaim(IdentityFieldEnum.TermsAcceptDate, DateTime.Now.AddMinutes(5).ToString("o"));
            await RefreshClaim(IdentityFieldEnum.TermsAccepted, true);
            return RedirectToAction("Index", "Home");
        }

        #endregion
    }
}
