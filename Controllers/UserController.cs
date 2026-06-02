using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public async Task<IActionResult> UpdateUserStatus(long id)
        {
            var user = await UserService.Get(id, [nameof(UserEntity.Id), nameof(UserEntity.Status)]);

            if (user is null)
            {
                return NotFound();
            }

            if (HasClaim(ClaimTypes.NameIdentifier, user.Id))
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

        #endregion

        #region Profile

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            //var user = await userService.GetValidUser(UserId);
            //if (user is null)
            //{
            //    return RedirectToAction("Index", "Home");
            //}

            //ViewBag.User = user;
            //ViewBag.Avatar = await userService.GetAvatar(user);

            //var serviceTermsFile = await fileService.Get(user.ServiceTermsFileId);
            //ViewBag.ServiceTermsFile = serviceTermsFile;
            //ViewBag.ServiceTermsUrl = fileService.GetUrl(serviceTermsFile);

            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SavePicture(IFormFile? picture)
        {
            //var user = await userService.GetValidUser(UserId);
            //if (user is null)
            //{
            //    return Unauthorized();
            //}

            //if (picture is null)
            //{
            //    if (user.ProfilePictureFileId.HasValue)
            //    {
            //        await fileService.Delete(user.ProfilePictureFileId.Value);
            //        await userService.UpdatePicture(user.Id, null);
            //    }
            //    HttpContext.Session.Remove(ConstantEnum.SessionUserPictureId);
            //    return Ok();
            //}

            //var file = await fileService.Create(
            //    picture,
            //    user.ProfilePictureFileId,
            //    3,
            //    [FileTypeEnum.PNG, FileTypeEnum.JPEG]
            //);

            //if (file.Message is not null)
            //{
            //    return BadRequest(new { file.Message });
            //}

            //await userService.UpdatePicture(user.Id, file.ObjectId);

            //HttpContext.Session.SetString(ConstantEnum.SessionUserPictureId, (file.ObjectId ?? 0).ToString());

            return Ok();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveTheme(string theme)
        {
            //if (long.TryParse(HttpContext.Session.GetString(ConstantEnum.SessionUserId), out var userId))
            //{
            //    await userService.UpdateTheme(
            //        userId,
            //        theme == UserThemeEnum.Light || theme == UserThemeEnum.Dark ? theme : UserThemeEnum.Light
            //    );
            //    HttpContext.Session.SetString(ConstantEnum.SessionUserTheme, theme);
            //}

            return Ok();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SavePersonal(SavePersonalViewModel model)
        {
            //var user = await userService.GetValidUser(UserId);
            //if (user is null)
            //{
            //    return RedirectToAction("Index", "Home");
            //}

            //if (string.IsNullOrWhiteSpace(model?.Name) || string.IsNullOrWhiteSpace(model?.Mobile))
            //{
            //    TempData["PersonalError"] = "Name and phone number are required.";
            //    return RedirectToAction(nameof(Profile));
            //}

            //// define service terms
            //var serviceTermsFileId = user.Role == UserRoleEnum.Company ? user.ServiceTermsFileId : null;
            //if (user.Role == UserRoleEnum.Company)
            //{
            //    if (model.DeleteServiceTerms && user.ServiceTermsFileId.HasValue)
            //    {
            //        await fileService.Delete(user.ServiceTermsFileId.Value);
            //        serviceTermsFileId = null;
            //    }
            //    if (model.ServiceTermsFile is not null)
            //    {
            //        var file = await fileService.Create(
            //            model.ServiceTermsFile,
            //            model.DeleteServiceTerms ? null : user.ServiceTermsFileId,
            //            5,
            //            [FileTypeEnum.PDF]
            //        );
            //        if (file.Message is not null)
            //        {
            //            TempData["PersonalError"] = file.Message;
            //            return RedirectToAction(nameof(Profile));
            //        }
            //        serviceTermsFileId = file.ObjectId;
            //    }
            //}

            //var number = user.Role == UserRoleEnum.Company ? user.Number : model.Number;
            //var interests = user.Role == UserRoleEnum.Company ? model?.Interests ?? [] : [];

            //await userService.UpdatePersonal(new UserEntity
            //{
            //    Id = user.Id,
            //    Number = number,
            //    Interests = interests,
            //    Name = model?.Name?.Trim() ?? "",
            //    Mobile = model?.Mobile?.Trim() ?? "",
            //    Address = model?.Address?.Trim(),
            //    ServiceTermsFileId = serviceTermsFileId
            //});

            //HttpContext.Session.SetString(ConstantEnum.SessionUserName, model?.Name?.Trim() ?? "-");

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string? newPassword, string? confirmPassword)
        {
            //var user = await userService.GetValidUser(UserId);
            //if (user is null)
            //{
            //    return RedirectToAction("Index", "Home");
            //}

            //if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            //{
            //    TempData["PasswordError"] = "All password fields are required.";
            //    return RedirectToAction(nameof(Profile));
            //}

            //if (newPassword != confirmPassword)
            //{
            //    TempData["PasswordError"] = "New passwords do not match.";
            //    return RedirectToAction(nameof(Profile));
            //}

            //var (hash, salt) = PasswordTool.HashPassword(newPassword);
            //await userService.UpdatePassword(user.Id, hash, salt);

            //HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        #endregion

        #region Terms

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptTerms()
        {
            await UserService.SetTermsAcceptDate(GetClaim<long>(ClaimTypes.NameIdentifier));
            await RefreshClaim("TermsAcceptDate", DateTime.Now);
            return RedirectToAction("Index", "Home");
        }

        #endregion
    }
}
