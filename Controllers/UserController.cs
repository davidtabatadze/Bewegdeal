using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Bewegdeal.Models;
using Bewegdeal.Services;
using Bewegdeal.Tools;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class UserController(FileService fileService, UserService userService) : XBaseController
    {

        #region List

        [RequireAdmin]
        public async Task<IActionResult> List()
        {
            var users = await userService.Load(new UserFilter() { Id = 0 });
            ViewBag.TotalCount = users.Count;
            ViewBag.CustomerCount = users.Count(u => u.Role == UserRoleEnum.Customer);
            ViewBag.CompanyCount = users.Count(u => u.Role == UserRoleEnum.Company);
            ViewBag.PendingCount = users.Count(u => u.Status == UserStatusEnum.Pending);
            return View();
        }

        [RequireAdmin]
        [HttpGet]
        public async Task<IActionResult> LoadUsers([FromQuery] UserFilter filter, [FromQuery] int draw = 1)
        {
            var users = await userService.Load(filter);
            var filtered = await userService.Count(filter);
            var total = await userService.Count(new UserFilter());

            var data = users.Select(u => new
            {
                id = u.Id,
                name = u.Name,
                email = u.Email,
                mobile = u.Mobile,
                address = u.Address,
                role = u.Role,
                status = u.Status,
                interests = u.Interests
            });

            return Json(new GridResultViewModel<object>(draw, total, filtered, data));
        }

        [RequireAdmin]
        [HttpPost]
        public async Task<IActionResult> UpdateUserStatus(long id)
        {
            if (id.ToString() == HttpContext.Session.GetString(ConstantEnum.SessionUserId))
            {
                return BadRequest();
            }

            var user = await userService.GetUser(id);

            if (user is null)
            {
                return NotFound();
            }

            var newStatus = user.Status switch
            {
                UserStatusEnum.Active => UserStatusEnum.Blocked,
                UserStatusEnum.Blocked => UserStatusEnum.Active,
                UserStatusEnum.Pending => UserStatusEnum.Active,
                _ => user.Status
            };

            await userService.SetUserStatus(id, newStatus);
            return Json(new { status = newStatus });
        }

        #endregion

        #region Profile

        [RequireLogin]
        public async Task<IActionResult> Profile()
        {
            var user = await userService.GetValidUser(UserId);
            if (user is null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.User = user;
            ViewBag.Avatar = await userService.GetAvatar(user);

            var serviceTermsFile = await fileService.Get(user.ServiceTermsFileId);
            ViewBag.ServiceTermsFile = serviceTermsFile;
            ViewBag.ServiceTermsUrl = fileService.GetFileUrl(serviceTermsFile);

            return View();
        }

        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> SavePicture(IFormFile? picture)
        {
            var user = await userService.GetValidUser(UserId);
            if (user is null)
            {
                return Unauthorized();
            }

            if (picture is null)
            {
                if (user.ProfilePictureFileId.HasValue)
                {
                    await fileService.Delete(user.ProfilePictureFileId.Value);
                    await userService.UpdatePicture(user.Id, null);
                }
                HttpContext.Session.Remove(ConstantEnum.SessionUserPictureId);
                return Ok();
            }

            var file = await fileService.Create(
                picture,
                user.ProfilePictureFileId,
                3,
                [FileTypeEnum.PNG, FileTypeEnum.JPEG]
            );

            if (file.Error is not null)
            {
                return BadRequest(new { file.Error });
            }

            await userService.UpdatePicture(user.Id, file.ObjectId);

            HttpContext.Session.SetString(ConstantEnum.SessionUserPictureId, (file.ObjectId ?? 0).ToString());

            return Ok();
        }

        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> SaveTheme(string theme)
        {
            if (long.TryParse(HttpContext.Session.GetString(ConstantEnum.SessionUserId), out var userId))
            {
                await userService.UpdateTheme(
                    userId,
                    theme == UserThemeEnum.Light || theme == UserThemeEnum.Dark ? theme : UserThemeEnum.Light
                );
                HttpContext.Session.SetString(ConstantEnum.SessionUserTheme, theme);
            }

            return Ok();
        }

        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> SavePersonal(SavePersonalViewModel model)
        {
            var user = await userService.GetValidUser(UserId);
            if (user is null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(model?.Name) || string.IsNullOrWhiteSpace(model?.Mobile))
            {
                TempData["PersonalError"] = "Name and phone number are required.";
                return RedirectToAction(nameof(Profile));
            }

            // define service terms
            var serviceTermsFileId = user.Role == UserRoleEnum.Company ? user.ServiceTermsFileId : null;
            if (user.Role == UserRoleEnum.Company)
            {
                if (model.DeleteServiceTerms && user.ServiceTermsFileId.HasValue)
                {
                    await fileService.Delete(user.ServiceTermsFileId.Value);
                    serviceTermsFileId = null;
                }
                if (model.ServiceTermsFile is not null)
                {
                    var file = await fileService.Create(
                        model.ServiceTermsFile,
                        model.DeleteServiceTerms ? null : user.ServiceTermsFileId,
                        5,
                        [FileTypeEnum.PDF]
                    );
                    if (file.Error is not null)
                    {
                        TempData["PersonalError"] = file.Error;
                        return RedirectToAction(nameof(Profile));
                    }
                    serviceTermsFileId = file.ObjectId;
                }
            }

            var number = user.Role == UserRoleEnum.Company ? user.Number : model.Number;
            var interests = user.Role == UserRoleEnum.Company ? model?.Interests ?? [] : [];

            await userService.UpdatePersonal(new UserEntity
            {
                Id = user.Id,
                Number = number,
                Interests = interests,
                Name = model?.Name?.Trim() ?? "",
                Mobile = model?.Mobile?.Trim() ?? "",
                Address = model?.Address?.Trim(),
                ServiceTermsFileId = serviceTermsFileId
            });

            HttpContext.Session.SetString(ConstantEnum.SessionUserName, model?.Name?.Trim() ?? "-");

            return RedirectToAction(nameof(Profile));
        }

        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string? newPassword, string? confirmPassword)
        {
            var user = await userService.GetValidUser(UserId);
            if (user is null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["PasswordError"] = "All password fields are required.";
                return RedirectToAction(nameof(Profile));
            }

            if (newPassword != confirmPassword)
            {
                TempData["PasswordError"] = "New passwords do not match.";
                return RedirectToAction(nameof(Profile));
            }

            var (hash, salt) = PasswordTool.HashPassword(newPassword);
            await userService.UpdatePassword(user.Id, hash, salt);

            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        #endregion
    }
}
