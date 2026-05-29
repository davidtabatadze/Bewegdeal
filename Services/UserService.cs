using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Models;

namespace Bewegdeal.Services
{
    public class UserService(IUserRepository userRepository, FileService fileService)
    {

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
