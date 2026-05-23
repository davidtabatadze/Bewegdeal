using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class XBaseController(IUserRepository userRepository) : Controller
    {
        protected async Task<UserEntity?> GetUser(string email)
        {
            return await userRepository.Get(new UserFilter { Email = (email ?? "").Trim() });
        }

        protected async Task<UserEntity?> GetUser(
            List<string>? roles = null,
            bool? active = null,
            bool? hiw = null
        )
        {
            if (!long.TryParse(HttpContext.Session.GetString(ConstantEnum.SessionUserId), out var id))
            {
                return null;
            }

            var user = await userRepository.Get(new UserFilter { Id = id });

            if (user is null)
            {
                return null;
            }

            if (roles is not null && !roles.Contains(user.Role))
            {
                return null;
            }

            if (hiw is not null && user.AcquaintedHIW != hiw)
            {
                return null;
            }

            if (active is not null && user.Status != UserStatusEnum.Active)
            {
                return null;
            }

            return user;
        }

        //protected async Task<UserPictureViewModel> GetUserPicture()
        //{
        //    var user = await GetUser();
        //    return user is null ? new UserPictureViewModel { } : await GetUserPicture(user);
        //}

        //protected async Task<UserPictureViewModel> GetUserPicture(long id)
        //{
        //    var pictures = await LoadUserPictures([id]);
        //    return pictures.FirstOrDefault() ?? new UserPictureViewModel { };
        //}

        //protected async Task<UserPictureViewModel> GetUserPicture(UserEntity user)
        //{
        //    var pictures = await LoadUserPictures([user]);
        //    return pictures.FirstOrDefault() ?? new UserPictureViewModel { };
        //}

        //protected async Task<List<UserPictureViewModel>> LoadUserPictures(List<long> ids)
        //{
        //    var users = await userRepository.Load(new UserFilter { Ids = ids });
        //    return await LoadUserPictures(users);
        //}

        //protected async Task<List<UserPictureViewModel>> LoadUserPictures(List<UserEntity> users)
        //{
        //    var fileIds = users.Where(u => u.ProfilePictureFileId.HasValue)
        //                       .Select(u => u.ProfilePictureFileId.Value)
        //                       .ToList();
        //    var files = await fileRepository.Load(new BaseFilter<long> { Ids = fileIds });

        //    return users.Select(u =>
        //    {
        //        var parts = u.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //        var initials = string.Concat(parts.Take(2).Select(p => char.ToUpper(p[0])));
        //        return new UserPictureViewModel
        //        {
        //            Initials = string.IsNullOrWhiteSpace(initials) ? "?" : initials,
        //            Url = Url.Action("Download", "File", new { key = file.Key })
        //        };
        //    }).ToList();


        //    var name = user?.Name ?? "-";
        //    var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //    var initials = string.Concat(parts.Take(2).Select(p => char.ToUpper(p[0])));
        //    if (string.IsNullOrEmpty(initials)) { initials = "?"; }

        //    string? pictureUrl = null;
        //    if (user?.ProfilePictureFileId.HasValue == true)
        //    {
        //        var file = await fileRepository.Get(user.ProfilePictureFileId.Value);
        //        if (file is not null)
        //        {
        //            pictureUrl = Url.Action("Download", "File", new { key = file.Key });
        //        }
        //    }

        //    return (name, initials, pictureUrl);
        //}

    }
}
