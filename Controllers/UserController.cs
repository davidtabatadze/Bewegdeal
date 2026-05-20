using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Bewegdeal.Models;
using Bewegdeal.Services;
using Bewegdeal.Tools;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class UserController(
        IUserRepository userRepository,
        IFileRepository fileRepository,
        FileService fileService
    ) : XBaseController(userRepository)
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IFileRepository _fileRepository = fileRepository;
        private readonly FileService _fileService = fileService;

        #region List

        [RequireAdmin]
        public async Task<IActionResult> List()
        {
            var users = await _userRepository.Load(new UserFilter() { Id = 0 });
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
            var users = await _userRepository.Load(filter);
            var filtered = await _userRepository.Count(filter);
            var total = await _userRepository.Count(new UserFilter());

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

            var user = await _userRepository.Get(new UserFilter { Id = id });

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

            await _userRepository.SetUserStatus(id, newStatus);
            return Json(new { status = newStatus });
        }

        #endregion

        #region Profile

        [RequireLogin]
        public async Task<IActionResult> Profile()
        {
            var user = await GetUser();
            if (user is null)
            {
                return RedirectToAction("Index", "Home");
            }

            FileEntity? picture = null;
            if (user.ProfilePictureFileId.HasValue)
            {
                picture = await _fileRepository.Get(user.ProfilePictureFileId.Value);
            }

            FileEntity? serviceTermsFile = null;
            if (user.Role == UserRoleEnum.Company && user.ServiceTermsFileId.HasValue)
            {
                serviceTermsFile = await _fileRepository.Get(user.ServiceTermsFileId.Value);
            }

            ViewBag.User = user;
            ViewBag.PictureUrl = picture is not null
                ? Url.Action("Download", "File", new { key = picture.Key })
                : null;
            ViewBag.ServiceTermsFile = serviceTermsFile;
            ViewBag.ServiceTermsUrl = serviceTermsFile is not null
                ? Url.Action("Download", "File", new { key = serviceTermsFile.Key })
                : null;

            return View();
        }

        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> SavePicture(IFormFile? picture)
        {
            var user = await GetUser();
            if (user is null)
            {
                return Unauthorized();
            }

            if (picture is null)
            {
                if (user.ProfilePictureFileId.HasValue)
                {
                    await _fileService.Delete(user.ProfilePictureFileId.Value);
                    await _userRepository.UpdatePicture(user.Id, null);
                }
                HttpContext.Session.Remove(ConstantEnum.SessionUserPictureKey);
                return Ok();
            }

            var (id, error) = await _fileService.Create(
                picture,
                user.ProfilePictureFileId,
                3,
                [FileTypeEnum.PNG, FileTypeEnum.JPEG]
            );

            if (error is not null)
            {
                return BadRequest(new { error });
            }

            await _userRepository.UpdatePicture(user.Id, id);

            var file = await _fileRepository.Get(id!.Value);
            if (file is not null)
            {
                HttpContext.Session.SetString(ConstantEnum.SessionUserPictureKey, file.Key);
            }

            return Ok();
        }

        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> SaveTheme(string theme)
        {
            if (long.TryParse(HttpContext.Session.GetString(ConstantEnum.SessionUserId), out var userId))
            {
                await _userRepository.UpdateTheme(
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
            var user = await GetUser();
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
                    await _fileService.Delete(user.ServiceTermsFileId.Value);
                    serviceTermsFileId = null;
                }
                if (model.ServiceTermsFile is not null)
                {
                    var file = await _fileService.Create(
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
                    serviceTermsFileId = file.Id;
                }
            }

            var number = user.Role == UserRoleEnum.Company ? user.Number : model.Number;
            var interests = user.Role == UserRoleEnum.Company ? model?.Interests ?? [] : [];

            await _userRepository.UpdatePersonal(new UserEntity
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
            var user = await GetUser();
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
            await _userRepository.UpdatePassword(user.Id, hash, salt);

            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        #endregion
    }
}
