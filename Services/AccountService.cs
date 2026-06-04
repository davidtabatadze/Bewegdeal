using Bewegdeal.Data.Entities;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Tools;
using Bewegdeal.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace Bewegdeal.Services
{
    public class AccountService(UserService UserService, FileService FileService, BrevoService BrevoService, IMemoryCache cache)
    {
        public async Task<ResultObjectModel<UserEntity>> Login(string email, string password)
        {
            var user = await UserService.Get(email);

            if (user is null || !PasswordTool.Verify(password, user.Password, user.Salt))
            {
                return ResultObjectModel<UserEntity>.Fail(AnnotationEnum.Account.Login.Credentials);
            }

            return user.Status switch
            {
                UserStatusEnum.Blocked => ResultObjectModel<UserEntity>.Fail(AnnotationEnum.Account.Login.Blocked),
                UserStatusEnum.Pending => ResultObjectModel<UserEntity>.Fail(AnnotationEnum.Account.Login.Pending),
                UserStatusEnum.Unverified => ResultObjectModel<UserEntity>.Fail(user, AnnotationEnum.Account.Login.Unverified),
                _ => ResultObjectModel<UserEntity>.Ok(user)
            };
        }

        public async Task<ResultModel> ForgotPassword(string email, string token, string resetLink)
        {
            var user = await UserService.Get(email, [nameof(UserEntity.Email), nameof(UserEntity.Name)]);

            if (user is not null)
            {
                // cache 
                cache.Set(
                    CacheKeyTool.Get(CacheKeyEnum.PasswordReset, token),
                    email,
                    TimeSpan.FromMinutes(ConstantEnum.ResetPasswordTimeout)
                );
                cache.Set(
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
                    return ResultModel.Fail(AnnotationEnum.Account.Email.Reset);
                }
            }

            return ResultModel.Ok(AnnotationEnum.Account.ForgotPassword.Success);
        }

        public async Task<ResultModel> ResetPassword(string token, string password)
        {
            // load cache
            var tokenKey = CacheKeyTool.Get(CacheKeyEnum.PasswordReset, token ?? "-");
            var email = cache.Get<string>(tokenKey) ?? "-";
            var emailKey = CacheKeyTool.Get(CacheKeyEnum.PasswordReset, email);
            var lastToken = cache.Get<string>(emailKey) ?? "-";

            // clear cache
            cache.Remove(tokenKey);
            cache.Remove(emailKey);

            // load user
            var user = await UserService.Get(email, [nameof(UserEntity.Id)]);

            // validate
            if (user is null || lastToken != token)
            {
                return ResultModel.Fail(AnnotationEnum.Account.ResetPassword.Expired);
            }

            // update password and clear token
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

            return ResultModel.Ok(AnnotationEnum.Account.ResetPassword.Success);
        }

        public async Task<ResultModel> VerifyAccount(string email, string mobile, string emailOtp, string mobileOtp)
        {
            // email code
            var emailCacheKey = CacheKeyTool.Get(CacheKeyEnum.EmailVerification, email);
            var cachedEmailOtp = cache.Get<string>(emailCacheKey);

            // mobile code
            var smsCacheKey = CacheKeyTool.Get(CacheKeyEnum.SmsVerification, mobile);
            var cachedMobileOtp = cache.Get<string>(smsCacheKey);

            // expired ...
            if (cachedEmailOtp is null || cachedMobileOtp is null)
            {
                return ResultModel.Fail(AnnotationEnum.Account.VerifyEmail.Expired);
            }

            // invalid ...
            if (cachedEmailOtp != emailOtp)
            {
                return ResultModel.Fail(AnnotationEnum.Account.VerifyEmail.InvalidEmail);
            }
            if (cachedMobileOtp != mobileOtp)
            {
                return ResultModel.Fail(AnnotationEnum.Account.VerifyEmail.InvalidMobile);
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
            cache.Remove(emailCacheKey);
            cache.Remove(smsCacheKey);
            return ResultModel.Ok(AnnotationEnum.Account.VerifyEmail.Success);
        }

        public async Task<ResultModel> VerifySend(string email, string mobile)
        {
            // generate ot code
            var otSms = Random.Shared.Next(100000, 1000000).ToString();
            var otEmail = Random.Shared.Next(100000, 1000000).ToString();

            // cache the code for later verification
            cache.Set(
                CacheKeyTool.Get(CacheKeyEnum.SmsVerification, mobile),
                otSms,
                TimeSpan.FromMinutes(
                    Convert.ToInt64(ConstantEnum.VerificationTimeout)
                )
            );
            cache.Set(
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
                return ResultModel.Fail(AnnotationEnum.Account.Sms.Verification);
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
                return ResultModel.Fail(AnnotationEnum.Account.Email.Verification);
            }

            return ResultModel.Ok(AnnotationEnum.Account.VerifyEmail.Resent);
        }

        public async Task<ResultModel> Register(RegistrationViewModel model)
        {
            // validate email uniqueness
            var existing = await UserService.GetRegistered(model.Email, model.Mobile);
            if (existing is not null)
            {
                return ResultModel.Fail(AnnotationEnum.Account.Register.Exists);
            }

            // ready terms of service
            long? termsFileId = null;
            if (model.Role == UserRoleEnum.Company && model.TermsFile is not null)
            {
                var file = await FileService.Create(model.TermsFile, null, 5, [FileTypeEnum.PDF]);
                if (file.Message is not null)
                {
                    return ResultModel.Fail(file.Message);
                }
                termsFileId = file.ObjectId;
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
                ServiceTermsFileId = termsFileId,
                AcquaintedHIW = false,
                Theme = model.Theme == UserThemeEnum.Dark ? UserThemeEnum.Dark : UserThemeEnum.Light,
                CreateDate = DateTime.Now,
                TermsAndConditionsAcceptDate = DateTime.Now
            });

            // send verification email
            var verification = await VerifySend(user.Email, user.Mobile);
            if (!verification.Success)
            {
                return ResultModel.Fail(verification.Message);
            }

            return ResultModel.Ok();
        }
    }
}
