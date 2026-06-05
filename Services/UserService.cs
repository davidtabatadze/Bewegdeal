using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.ViewModels;

namespace Bewegdeal.Services
{
    public class UserService(IUserRepository UserRepository, FileService FileService)
    {

        #region Repository

        public async Task<UserEntity> Create(UserEntity user)
            => await UserRepository.Create(user);

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

        public async Task<List<UserEntity>> Load(UserFilter filter)
            => await UserRepository.Load(filter);

        #endregion

        public async Task<ResultModel> UpdateProfile(long id, ProfileViewModel model)
        {
            var user = await Get(id, [nameof(UserEntity.Id), nameof(UserEntity.Role), nameof(UserEntity.ServiceTermsFileId)]);
            if (user is null || user.Role != model.Role)
            {
                return ResultModel.Fail("");
            }

            // define service terms
            var serviceTermsFileId = user.Role == UserRoleEnum.Company ? user.ServiceTermsFileId : null;
            if (user.Role == UserRoleEnum.Company)
            {
                if (model.DeleteServiceTerms && user.ServiceTermsFileId.HasValue)
                {
                    await FileService.Delete(user.ServiceTermsFileId.Value);
                    serviceTermsFileId = null;
                }
                if (model.ServiceTermsFile is not null)
                {
                    var file = await FileService.Create(
                        model.ServiceTermsFile,
                        model.DeleteServiceTerms ? null : user.ServiceTermsFileId,
                        5,
                        [FileTypeEnum.PDF]
                    );
                    if (file.Message is not null)
                    {
                        return ResultModel.Fail(file.Message);
                    }
                    serviceTermsFileId = file.ObjectId;
                }
            }

            // save
            await Update(UserUpdateAreaEnum.Profile, new UserEntity
            {
                Id = user.Id,
                Name = model.Name,
                Address = model.Address,
                Interests = model.Interests ?? [],
                ServiceTermsFileId = serviceTermsFileId
            });

            return ResultModel.Ok();
        }

        public async Task<ResultModel> UpdateAvatar(long id, IFormFile? avatar)
        {
            var user = await Get(id, [nameof(UserEntity.Id), nameof(UserEntity.AvatarFileId)]);
            if (user is null)
            {
                return ResultModel.Fail("");
            }

            // define file
            var fileId = (long?)null;
            var fileUrl = string.Empty;

            if (avatar is null)
            {
                await FileService.Delete(user.AvatarFileId);
            }
            else
            {
                var file = await FileService.Create(
                    avatar,
                    user.AvatarFileId,
                    3,
                    [FileTypeEnum.PNG, FileTypeEnum.JPEG]
                );
                if (file.Message is not null)
                {
                    return ResultModel.Fail(file.Message);
                }
                fileId = file.ObjectId;
                fileUrl = await FileService.GetUrl(fileId);
            }

            await Update(UserUpdateAreaEnum.Avatar, new UserEntity
            {
                Id = user.Id,
                AvatarFileId = fileId
            });

            return ResultModel.Ok(fileUrl);
        }

        public async Task<UserProfileModel?> GetProfile(long id)
        {
            var user = (await Get(id)) ?? new UserEntity { };
            if (user.Id == 0)
            {
                return null;
            }

            var serviceTermsFile = await FileService.Get(user.ServiceTermsFileId);
            var serviceTermsFileUrl = await FileService.GetUrl(user.ServiceTermsFileId);

            return new UserProfileModel
            {
                User = user,
                ServiceTermsFileUrl = serviceTermsFileUrl,
                ServiceTermsFileName = serviceTermsFile?.FileName,
                Avatar = await GetAvatar(user)
            };
        }

        public async Task<UserAvatarModel> GetAvatar(long? id)
            => await GetAvatar(await Get(
                id ?? 0,
                [nameof(UserEntity.Id), nameof(UserEntity.Name), nameof(UserEntity.AvatarFileId)]
            ));

        public async Task<UserAvatarModel> GetAvatar(UserEntity? user)
        {
            var avatar = await FileService.Get(user?.AvatarFileId);
            return GetAvatar(user, avatar);
        }

        public UserAvatarModel GetAvatar(UserEntity? user, FileEntity? file)
        {
            var avatar = new UserAvatarModel();

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
            }

            if (file is not null)
            {
                avatar.Url = FileService.GetUrl(file);
            }

            return avatar;
        }

        public async Task<GridResultViewModel<object>> LoadGrid(UserFilter filter, int draw)
        {
            var users = await Load(filter);
            var filtered = await Count(filter);
            var total = await Count(new UserFilter());
            var avatarFiles = await FileService.Load(new BaseFilter
            {
                Ids = [.. users.Where(u => u.AvatarFileId.HasValue).Select(u => u.AvatarFileId!.Value)]
            });
            var avatars = users.Select(u =>
            {
                var file = avatarFiles.FirstOrDefault(f => f.Id == u.AvatarFileId);
                return GetAvatar(u, file);
            }).ToList();

            return new GridResultViewModel<object>
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
