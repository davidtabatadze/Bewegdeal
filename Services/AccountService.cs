using Bewegdeal.Data.Entities;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Tools;
using Microsoft.Extensions.Caching.Memory;

namespace Bewegdeal.Services
{
    public class AccountService(UserService UserService, FileService FileService, MailService MailService, IMemoryCache cache)
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
                UserStatusEnum.Unverified => ResultObjectModel<UserEntity>.Fail(AnnotationEnum.Account.Login.Unverified),
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
                var result = await MailService.Send(
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

        public async Task<ResultModel> VerifyEmail(string email, string otp)
        {
            // seek one time code
            var otCodeKey = CacheKeyTool.Get(CacheKeyEnum.EmailVerification, email);
            var otCode = cache.Get<string>(otCodeKey);

            // no code? error
            if (otCode is null)
            {
                return ResultModel.Fail(AnnotationEnum.Account.VerifyEmail.Expired);
            }

            // wrong input? error
            if (otCode != otp)
            {
                return ResultModel.Fail(AnnotationEnum.Account.VerifyEmail.Invalid);
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
            cache.Remove(otCodeKey);
            return ResultModel.Ok(AnnotationEnum.Account.VerifyEmail.Success);
        }

        public async Task<ResultModel> VerifySend(string email)
        {
            // generate ot code
            var otCode = Random.Shared.Next(100000, 1000000).ToString();

            // cache the code for later verification
            cache.Set(
                CacheKeyTool.Get(CacheKeyEnum.EmailVerification, email),
                otCode,
                TimeSpan.FromMinutes(
                    Convert.ToInt64(ConstantEnum.EmailVerificationTimeout)
                )
            );

            // send email
            var result = await MailService.Send(
                email,
                EmailEnum.VerifyAccount,
                new Dictionary<string, object> {
                    { "otcode", otCode },
                    { "timeout", ConstantEnum.EmailVerificationTimeout }
                }
            );

            if (!result.Success)
            {
                return ResultModel.Fail(AnnotationEnum.Account.Email.Verification);
            }

            return ResultModel.Ok(AnnotationEnum.Account.VerifyEmail.Resent);
        }

        public async Task<ResultModel> Register(RegisterViewModel model)
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
            var verification = await VerifySend(user.Email);
            if (!verification.Success)
            {
                return ResultModel.Fail(verification.Message);
            }

            return ResultModel.Ok();
        }
    }
}
