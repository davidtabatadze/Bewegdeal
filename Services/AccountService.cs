using Bewegdeal.Data.Entities;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Tools;
using Bewegdeal.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace Bewegdeal.Services
{
    public class AccountService(UserService UserService, FileService FileService, SettingService SettingService, BrevoService BrevoService, IMemoryCache Cache)
    {
        public async Task<GenericResultModel<UserEntity>> Login(string email, string password)
        {
            var user = await UserService.Get(email, [
                nameof(UserEntity.Id),
                nameof(UserEntity.Role),
                nameof(UserEntity.Name),
                nameof(UserEntity.Email),
                nameof(UserEntity.Mobile),
                nameof(UserEntity.Status),
                nameof(UserEntity.Password),
                nameof(UserEntity.Salt),
                nameof(UserEntity.Theme),
                nameof(UserEntity.Avatar),
                nameof(UserEntity.AcquaintedHIW),
                nameof(UserEntity.TermsAndConditionsAcceptDate),
            ]);

            if (user is null || !PasswordTool.Verify(password, user.Password, user.Salt))
            {
                return GenericResultModel<UserEntity>.Fail(AnnotationEnum.Account.Login.Credentials);
            }

            return user.Status switch
            {
                UserStatusEnum.Blocked => GenericResultModel<UserEntity>.Fail(AnnotationEnum.Account.Login.Blocked),
                UserStatusEnum.Pending => GenericResultModel<UserEntity>.Fail(AnnotationEnum.Account.Login.Pending),
                UserStatusEnum.Unverified => GenericResultModel<UserEntity>.Fail(user, AnnotationEnum.Account.Login.Unverified),
                _ => GenericResultModel<UserEntity>.Ok(user)
            };
        }

        public async Task<GenericResultModel> ForgotPassword(string email, string token, string resetLink)
        {
            var user = await UserService.Get(email, [nameof(UserEntity.Email), nameof(UserEntity.Name)]);

            if (user is not null)
            {
                // cache 
                Cache.Set(
                    CacheKeyTool.Get(CacheKeyEnum.PasswordReset, token),
                    email,
                    TimeSpan.FromMinutes(ConstantEnum.ResetPasswordTimeout)
                );
                Cache.Set(
                    CacheKeyTool.Get(CacheKeyEnum.PasswordReset, email),
                    token,
                    TimeSpan.FromMinutes(ConstantEnum.ResetPasswordTimeout)
                );

                // send email
                var result = await BrevoService.SendEmail(
                    email,
                    EmailEnum.PasswordReset,
                    new Dictionary<string, object> {
                        { "name", user.Name },
                        { "resetLink", resetLink },
                        { "timeout", ConstantEnum.ResetPasswordTimeout }
                    }
                );

                if (!result.Success)
                {
                    return GenericResultModel.Fail(AnnotationEnum.Account.Email.Reset);
                }
            }

            return GenericResultModel.Ok(AnnotationEnum.Account.ForgotPassword.Success);
        }

        public async Task<GenericResultModel> ResetPassword(string token, string password)
        {
            // load cache
            var tokenKey = CacheKeyTool.Get(CacheKeyEnum.PasswordReset, token ?? "-");
            var email = Cache.Get<string>(tokenKey) ?? "-";
            var emailKey = CacheKeyTool.Get(CacheKeyEnum.PasswordReset, email);
            var lastToken = Cache.Get<string>(emailKey) ?? "-";

            // clear cache
            Cache.Remove(tokenKey);
            Cache.Remove(emailKey);

            // load user
            var user = await UserService.Get(email, [nameof(UserEntity.Id)]);

            // validate
            if (user is null || lastToken != token)
            {
                return GenericResultModel.Fail(AnnotationEnum.Account.ResetPassword.Expired);
            }

            // update password
            var (hash, salt) = PasswordTool.HashPassword(password);
            await UserService.Update(
                UserUpdateAreaEnum.Password,
                new UserEntity
                {
                    Id = user.Id,
                    Salt = salt,
                    Password = hash
                }
            );

            return GenericResultModel.Ok(AnnotationEnum.Account.ResetPassword.Success);
        }

        public async Task<GenericResultModel> VerifyAccount(string email, string mobile, string emailOtp, string mobileOtp)
        {
            // email code
            var emailCacheKey = CacheKeyTool.Get(CacheKeyEnum.EmailVerification, email);
            var cachedEmailOtp = Cache.Get<string>(emailCacheKey);

            // mobile code
            var smsCacheKey = CacheKeyTool.Get(CacheKeyEnum.SmsVerification, mobile);
            var cachedMobileOtp = Cache.Get<string>(smsCacheKey);

            // expired ...
            if (cachedEmailOtp is null || cachedMobileOtp is null)
            {
                return GenericResultModel.Fail(AnnotationEnum.Account.VerifyEmail.Expired);
            }

            // invalid ...
            if (cachedEmailOtp != emailOtp)
            {
                return GenericResultModel.Fail(AnnotationEnum.Account.VerifyEmail.InvalidEmail);
            }
            if (cachedMobileOtp != mobileOtp)
            {
                return GenericResultModel.Fail(AnnotationEnum.Account.VerifyEmail.InvalidMobile);
            }

            // update user
            var user = await UserService.Get(email, [nameof(UserEntity.Id), nameof(UserEntity.Role)]);
            if (user is not null)
            {
                await UserService.Update(
                    UserUpdateAreaEnum.Status,
                    new UserEntity
                    {
                        Id = user.Id,
                        Status = user.Role == UserRoleEnum.Customer ?
                        UserStatusEnum.Active : UserStatusEnum.Pending
                    }
                );
            }

            // clear cache
            Cache.Remove(emailCacheKey);
            Cache.Remove(smsCacheKey);
            return GenericResultModel.Ok(AnnotationEnum.Account.VerifyEmail.Success);
        }

        public async Task<GenericResultModel> VerifySend(string email, string mobile)
        {
            // generate ot code
            var otSms = Random.Shared.Next(100000, 1000000).ToString();
            var otEmail = Random.Shared.Next(100000, 1000000).ToString();

            // cache the code for later verification
            Cache.Set(
                CacheKeyTool.Get(CacheKeyEnum.SmsVerification, mobile),
                otSms,
                TimeSpan.FromMinutes(
                    Convert.ToInt64(ConstantEnum.VerificationTimeout)
                )
            );
            Cache.Set(
                CacheKeyTool.Get(CacheKeyEnum.EmailVerification, email),
                otEmail,
                TimeSpan.FromMinutes(
                    Convert.ToInt64(ConstantEnum.VerificationTimeout)
                )
            );

            // send sms
            var smsResult = await BrevoService.SendSms(
                mobile,
                new Dictionary<string, object> {
                    { "otcode", otSms },
                    { "timeout", ConstantEnum.VerificationTimeout }
                }
            );

            if (!smsResult.Success)
            {
                return GenericResultModel.Fail(AnnotationEnum.Account.Sms.Verification);
            }

            // send email
            var emailResult = await BrevoService.SendEmail(
                email,
                EmailEnum.VerifyAccount,
                new Dictionary<string, object> {
                    { "otcode", otEmail },
                    { "timeout", ConstantEnum.VerificationTimeout }
                }
            );

            if (!emailResult.Success)
            {
                return GenericResultModel.Fail(AnnotationEnum.Account.Email.Verification);
            }

            return GenericResultModel.Ok(AnnotationEnum.Account.VerifyEmail.Resent);
        }

        public async Task<GenericResultModel> Register(RegistrationViewModel model)
        {
            // fix mobile
            model.Mobile = model.Mobile.Replace(" ", "").Trim();

            var settings = await SettingService.GetCached();
            if (!string.IsNullOrWhiteSpace(settings.MobilePrefix))
            {
                model.Mobile = settings.MobilePrefix + model.Mobile;
            }

            // validate uniqueness
            var existing = await UserService.GetRegistered(model.Email, model.Mobile);
            if (existing is not null)
            {
                return GenericResultModel.Fail(AnnotationEnum.Account.Register.Exists);
            }

            // ready terms of service
            string? userServiceTerms = null;
            if (model.Role == UserRoleEnum.Company && model.TermsFile is not null)
            {
                var file = await FileService.Create(model.TermsFile, null, 5, [FileTypeEnum.PDF]);
                if (file.Message is not null)
                {
                    return GenericResultModel.Fail(file.Message);
                }
                userServiceTerms = file.Result;
            }

            // ready password
            var (hash, salt) = PasswordTool.HashPassword(model.Password);

            // do create user
            var user = await UserService.Create(new UserEntity
            {
                Role = model.Role,
                Name = model.Name,
                Email = model.Email,
                Number = model.Number,
                Mobile = model.Mobile,
                Address = model.Address,
                Password = hash,
                Salt = salt,
                Interests = model.Interests ?? [],
                Status = UserStatusEnum.Unverified,
                ServiceTerms = userServiceTerms,
                AcquaintedHIW = false,
                Theme = model.Theme == UserThemeEnum.Dark ? UserThemeEnum.Dark : UserThemeEnum.Light,
                CreateDate = DateTime.Now,
                TermsAndConditionsAcceptDate = DateTime.Now
            });

            // send verification
            var verification = await VerifySend(user.Email, user.Mobile);
            if (!verification.Success)
            {
                return GenericResultModel.Fail(verification.Message);
            }

            return GenericResultModel.Ok();
        }

        public UserAvatarModel GetAvatar(UserEntity? user) => UserService.GetAvatar(user);
    }
}
