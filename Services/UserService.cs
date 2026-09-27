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

        public async Task Update(UserUpdateAreaEnum area, UserEntity update, UserContactEntity? contact = null)
            => await UserRepository.Update(area, update, contact);

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

        public async Task<UserContactEntity?> GetContact(long contactId)
            => await UserRepository.GetContact(contactId);

        public async Task<List<UserContactEntity>> LoadContacts(IEnumerable<long> contactIds)
            => await UserRepository.LoadContacts(contactIds);

        #endregion

        public async Task<GenericResultModel> UpdateProfile(long id, ProfileViewModel model)
        {
            var user = await Get(id, [nameof(UserEntity.Id), nameof(UserEntity.Role), nameof(UserEntity.ContactId)]);
            if (user is null || user.Role != model.Role)
            {
                return GenericResultModel.Fail("");
            }

            if (user.Role == UserRoleEnum.Company)
            {
                var contact = await GetContact(user.ContactId);
                string? serviceTerms = contact?.ServiceTerms;

                if (model.DeleteServiceTerms)
                {
                    await FileService.Delete(serviceTerms);
                    serviceTerms = null;
                }

                if (model.ServiceTermsFile is not null)
                {
                    var file = await FileService.Create(
                        model.ServiceTermsFile,
                        serviceTerms,
                        5,
                        [FileTypeEnum.PDF]
                    );
                    if (file.Message is not null)
                    {
                        return GenericResultModel.Fail(file.Message);
                    }
                    serviceTerms = file.Result;
                }

                await Update(
                    UserUpdateAreaEnum.Contact,
                    new UserEntity
                    {
                        Id = user.Id
                    },
                    new UserContactEntity
                    {
                        Address = model.Address,
                        Owner = model.Owner,
                        City = model.City,
                        ZipCode = model.ZipCode,
                        ServiceTerms = serviceTerms
                    }
                );
            }

            await Update(UserUpdateAreaEnum.Profile, new UserEntity
            {
                Id = user.Id,
                Name = model.Name,
                Interests = model.Interests ?? []
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
                // return GenericResultModel.Fail("All password fields are required.");
                return GenericResultModel.Fail("Alle Passwortfelder sind erforderlich.");
            }
            if (newPassword != confirmPassword)
            {
                // return GenericResultModel.Fail("New passwords do not match.");
                return GenericResultModel.Fail("Die neuen Passwörter stimmen nicht überein.");
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

            var contact = await GetContact(user.ContactId);

            return new UserProfileModel
            {
                User = user,
                Contact = contact,
                ServiceTermsFileUrl = FileService.GetUrl(contact?.ServiceTerms),
                ServiceTermsFileName = FileService.GetName(contact?.ServiceTerms),
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

            filter.Start = null;
            filter.Length = null;

            var filtered = await Count(filter);
            var total = await Count(new UserFilter());
            var avatars = users.Select(u => GetAvatar(u)).ToList();
            var contacts = await LoadContacts(users.Select(u => u.ContactId));


            return new GridResultModel<object>
            {
                Draw = draw,
                RecordsTotal = total,
                RecordsFiltered = filtered,
                Data = users.Select((u, i) =>
                {
                    var contact = contacts.FirstOrDefault(c => c.Id == u.ContactId);
                    return new
                    {
                        id = u.Id,
                        name = u.Name,
                        email = u.Email,
                        mobile = u.Mobile,
                        role = u.Role,
                        status = u.Status,
                        avatar = avatars[i],
                        interests = u.Interests,
                        createDate = u.CreateDate.ToString("yyyy-MM-dd HH:mm"),
                        address = contact == null ? null :
                                  contact.Address + ", " + contact.ZipCode + "," + contact.City
                    };
                })
            };
        }

    }
}
