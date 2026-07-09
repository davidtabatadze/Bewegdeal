using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Tools;
using Bewegdeal.ViewModels;

namespace Bewegdeal.Services
{
    public class UserService(IUserRepository UserRepository, FileService FileService)
    {

        #region Repository

        public async Task<UserEntity> Create(UserEntity user)
            => await UserRepository.Create(user);

        public async Task Delete(long id)
            => await UserRepository.Delete<UserEntity>(id);

        public async Task Update(UserUpdateAreaEnum area, UserEntity update)
            => await UserRepository.Update(area, update);

        public async Task<UserEntity?> Get(long id, string[]? properties = null)
            => await UserRepository.Get<UserEntity>(id, properties);

        public async Task<UserEntity?> Get(string email, string[]? properties = null)
            => await UserRepository.Get(new UserFilter { Email = (email ?? "-").Trim() }, properties);

        public async Task<UserEntity?> GetRegistered(string email, string mobile)
            => await UserRepository.GetRegistered(email, mobile);

        public async Task<int> Count(UserFilter filter)
            => await UserRepository.Count(filter);

        public async Task<List<UserEntity>> Load(UserFilter filter, string[]? properties = null)
            => await UserRepository.Load(filter, properties);

        public async Task<List<UserEntity>> Load(IEnumerable<long> ids, string[]? properties = null)
            => await UserRepository.Load<UserEntity>(ids, properties);

        public async Task Rate(long userId, long evaluatorId, decimal value)
            => await UserRepository.Rate(userId, evaluatorId, value);

        #endregion

        public async Task<GenericResultModel> UpdateProfile(long id, ProfileViewModel model)
        {
            var user = await Get(id, [nameof(UserEntity.Id), nameof(UserEntity.Role), nameof(UserEntity.ServiceTerms)]);
            if (user is null || user.Role != model.Role)
            {
                return GenericResultModel.Fail("");
            }

            // define service terms
            var userServiceTerms = user.Role == UserRoleEnum.Company ? user.ServiceTerms : null;
            if (user.Role == UserRoleEnum.Company)
            {
                if (model.DeleteServiceTerms)
                {
                    await FileService.Delete(user.ServiceTerms);
                    userServiceTerms = null;
                }
                if (model.ServiceTermsFile is not null)
                {
                    var file = await FileService.Create(
                        model.ServiceTermsFile,
                        model.DeleteServiceTerms ? null : user.ServiceTerms,
                        5,
                        [FileTypeEnum.PDF]
                    );
                    if (file.Message is not null)
                    {
                        return GenericResultModel.Fail(file.Message);
                    }
                    userServiceTerms = file.Result;
                }
            }

            // save
            await Update(UserUpdateAreaEnum.Profile, new UserEntity
            {
                Id = user.Id,
                Name = model.Name,
                Address = model.Address,
                Interests = model.Interests ?? [],
                ServiceTerms = userServiceTerms
            });

            return GenericResultModel.Ok();
        }

        public async Task<GenericResultModel> UpdateAvatar(long id, IFormFile? avatar)
        {
            var user = await Get(id, [nameof(UserEntity.Id), nameof(UserEntity.Avatar)]);
            if (user is null)
            {
                return GenericResultModel.Fail("");
            }

            // define file
            string? userAvatar = null;
            if (avatar is null)
            {
                await FileService.Delete(user.Avatar);
            }
            else
            {
                var file = await FileService.Create(
                    avatar,
                    user.Avatar,
                    3,
                    [FileTypeEnum.PNG, FileTypeEnum.JPEG]
                );
                if (file.Message is not null)
                {
                    return GenericResultModel.Fail(file.Message);
                }
                userAvatar = file.Result;
            }

            await Update(UserUpdateAreaEnum.Avatar, new UserEntity
            {
                Id = user.Id,
                Avatar = userAvatar
            });

            return GenericResultModel.Ok(FileService.GetUrl(userAvatar));
        }

        public async Task<GenericResultModel> UpdatePassword(long id, string? newPassword, string? confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                return GenericResultModel.Fail("All password fields are required.");
            }
            if (newPassword != confirmPassword)
            {
                return GenericResultModel.Fail("New passwords do not match.");
            }

            // update password
            var (hash, salt) = PasswordTool.HashPassword(newPassword);
            await Update(
                UserUpdateAreaEnum.Password,
                new UserEntity
                {
                    Id = id,
                    Salt = salt,
                    Password = hash
                }
            );

            return GenericResultModel.Ok();
        }

        public async Task<UserProfileModel?> GetProfile(long id)
        {
            var user = (await Get(id)) ?? new UserEntity { };
            if (user.Id == 0)
            {
                return null;
            }

            var serviceTermsFileUrl = FileService.GetUrl(user.ServiceTerms);
            var serviceTermsFileName = FileService.GetName(user.ServiceTerms);

            return new UserProfileModel
            {
                User = user,
                ServiceTermsFileUrl = serviceTermsFileUrl,
                ServiceTermsFileName = serviceTermsFileName,
                Avatar = GetAvatar(user)
            };
        }

        public UserAvatarModel GetAvatar(UserEntity? user)
        {
            var avatar = new UserAvatarModel();

            if (user is not null)
            {
                avatar.Url = FileService.GetUrl(user.Avatar);
                avatar.Name = user.Name;
                avatar.Rating = user.Rating;
                avatar.Initials = string.Concat(
                    user.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Take(2).Select(p => char.ToUpper(p[0]))
                );
            }

            return avatar;
        }

        public async Task<GenericResultModel<dynamic>> LoadGrid()
        {
            var total = await Count(new UserFilter { Status = UserStatusEnum.Active });
            var customer = await Count(new UserFilter { Status = UserStatusEnum.Active, Role = UserRoleEnum.Customer });
            var company = await Count(new UserFilter { Status = UserStatusEnum.Active, Role = UserRoleEnum.Company });
            var pending = await Count(new UserFilter { Status = UserStatusEnum.Pending });

            return GenericResultModel<dynamic>.Ok(new { total, customer, company, pending });
        }

        public async Task<GridResultModel<object>> LoadGrid(UserFilter filter, int draw)
        {
            var users = await Load(filter);
            var filtered = await Count(filter);
            var total = await Count(new UserFilter());
            var avatars = users.Select(u => GetAvatar(u)).ToList();

            return new GridResultModel<object>
            {
                Draw = draw,
                RecordsTotal = total,
                RecordsFiltered = filtered,
                Data = users.Select((u, i) => new
                {
                    id = u.Id,
                    name = u.Name,
                    email = u.Email,
                    mobile = u.Mobile,
                    address = u.Address,
                    role = u.Role,
                    status = u.Status,
                    avatar = avatars[i],
                    interests = u.Interests,
                    createDate = u.CreateDate.ToString("yyyy-MM-dd HH:mm")
                })
            };
        }

    }
}
