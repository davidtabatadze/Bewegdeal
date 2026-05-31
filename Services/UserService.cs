using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Models;

namespace Bewegdeal.Services
{
    public class UserService(IUserRepository UserRepository, FileService fileService)
    {

        #region Repository

        public async Task<UserEntity> Create(UserEntity user)
            => await UserRepository.Create(user);

        public async Task Update(UserUpdateAreaEnum area, UserEntity update)
            => await UserRepository.Update(area, update);

        //public async Task SetUserStatus(long id, string status)
        //{
        //    await UserRepository.SetUserStatus(id, status);
        //}
        //public async Task SetAcquaintedHIW(long id)
        //{
        //    await UserRepository.SetAcquaintedHIW(id);
        //}
        //public async Task UpdatePassword(long id, string hash, string salt)
        //{
        //    await UserRepository.UpdatePassword(id, hash, salt);
        //}
        //public async Task UpdatePicture(long id, long? fileId)
        //{
        //    await UserRepository.UpdatePicture(id, fileId);
        //}
        //public async Task UpdateTheme(long id, string theme)
        //{
        //    await UserRepository.UpdateTheme(id, theme);
        //}
        //public async Task UpdatePersonal(UserEntity user)
        //{
        //    await UserRepository.UpdatePersonal(user);
        //}


        public async Task<UserEntity?> Get(long id, string[]? properties = null)
            => await UserRepository.Get<UserEntity>(id, properties);

        public async Task<UserEntity?> Get(string email, string[]? properties = null)
            => await UserRepository.Get(new UserFilter { Email = (email ?? "-").Trim() }, properties);

        public async Task<int> Count(UserFilter filter)
            => await UserRepository.Count(filter);

        public async Task<List<UserEntity>> Load(UserFilter filter)
            => await UserRepository.Load(filter);

        #endregion

        public async Task<UserEntity?> GetValidUser(long? id, List<string>? roles = null, bool? hiw = null)
        {
            var checkHIW = hiw is not null;
            var checkRole = roles is not null && roles.Count > 0;

            var properties = new List<string> {
                nameof(UserEntity.Id)
            };

            if (checkHIW)
            {
                properties.Add(nameof(UserEntity.AcquaintedHIW));
            }
            if (checkRole)
            {
                properties.Add(nameof(UserEntity.Role));
            }

            var user = await UserRepository.Get(
                new UserFilter { Id = id ?? 0, Status = UserStatusEnum.Active },
                [.. properties]
            );

            if (user is null || (checkHIW && user.AcquaintedHIW != hiw) || (checkRole && !(roles ?? []).Contains(user.Role)))
            {
                return null;
            }

            return user;
        }

        public async Task<UserAvatarViewModel> GetAvatar(long? userId)
            => await GetAvatar(await Get(
                userId ?? 0,
                [nameof(UserEntity.Id), nameof(UserEntity.Name), nameof(UserEntity.ProfilePictureFileId)]
            ));

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
                avatar.Url = await fileService.GetUrl(user.ProfilePictureFileId);
            }

            return avatar;
        }

        public async Task<GridResultViewModel<object>> LoadGrid(UserFilter filter, int draw)
        {
            var users = await Load(filter);
            var filtered = await Count(filter);
            var total = await Count(new UserFilter());

            return new GridResultViewModel<object>
            {
                Draw = draw,
                RecordsTotal = total,
                RecordsFiltered = filtered,
                Data = users.Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    email = u.Email,
                    mobile = u.Mobile,
                    address = u.Address,
                    role = u.Role,
                    status = u.Status,
                    interests = u.Interests
                })
            };
        }

    }
}
