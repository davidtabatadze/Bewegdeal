using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Models;

namespace Bewegdeal.Services
{
    public class UserService(IUserRepository userRepository, FileService fileService)
    {

        #region Repository

        public async Task<UserEntity> Create(UserEntity user)
        {
            return await userRepository.Create(user);
        }
        public async Task SetUserStatus(long id, string status)
        {
            await userRepository.SetUserStatus(id, status);
        }
        public async Task SetAcquaintedHIW(long id)
        {
            await userRepository.SetAcquaintedHIW(id);
        }
        public async Task UpdatePassword(long id, string hash, string salt)
        {
            await userRepository.UpdatePassword(id, hash, salt);
        }
        public async Task UpdatePicture(long id, long? fileId)
        {
            await userRepository.UpdatePicture(id, fileId);
        }
        public async Task UpdateTheme(long id, string theme)
        {
            await userRepository.UpdateTheme(id, theme);
        }
        public async Task UpdatePersonal(UserEntity user)
        {
            await userRepository.UpdatePersonal(user);
        }
        public async Task<UserEntity?> Get(UserFilter filter)
        {
            return await userRepository.Get(filter);
        }
        public async Task<int> Count(UserFilter filter)
        {
            return await userRepository.Count(filter);
        }
        public async Task<List<UserEntity>> Load(UserFilter filter)
        {
            return await userRepository.Load(filter);
        }

        #endregion

        public async Task<UserEntity?> GetUser(long id)
        {
            return await userRepository.Get(new UserFilter { Id = id });
        }

        public async Task<UserEntity?> GetUser(string email)
        {
            return await userRepository.Get(new UserFilter { Email = (email ?? "").Trim() });
        }

        public async Task<UserEntity?> GetValidUser(string? key, List<string>? roles = null, bool? active = null, bool? hiw = null)
        {
            if (string.IsNullOrWhiteSpace(key) || !long.TryParse(key, out var id))
            {
                return null;
            }

            var user = await GetUser(id);

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

        public async Task<UserAvatarViewModel> GetAvatar(long? userId)
        {
            var user = await userRepository.Get(new UserFilter { Id = userId ?? 0 });
            return await GetAvatar(user);
        }

        public async Task<UserAvatarViewModel> GetAvatar(UserEntity? user)
        {
            var avatar = new UserAvatarViewModel
            {
                Initials = "??",
                Name = "Undefined",
                Url = null
            };

            if (user is not null)
            {
                if (!string.IsNullOrWhiteSpace(user.Name))
                {
                    avatar.Name = user.Name;
                    avatar.Initials = string.Concat(
                        user.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                 .Take(2).Select(p => char.ToUpper(p[0]))
                    );
                }
                avatar.Url = await fileService.GetFileUrl(user.ProfilePictureFileId);
            }

            return avatar;
        }

    }
}
