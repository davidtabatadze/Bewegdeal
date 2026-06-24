using Bewegdeal.Data.Entities;
using Bewegdeal.Enums;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Bewegdeal.Tools
{
    public class UserRefreshTool(RequestDelegate next)
    {
        public async Task InvokeAsync(
            HttpContext context,
            UserService userService,
            SettingService settingService,
            IMemoryCache cache)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var status = UserStatusEnum.Active;
                var forceTC = context.User.FindFirstValue(IdentityFieldEnum.TermsAccepted)?.ToLower() != "true";
                var parse = long.TryParse(context.User.FindFirstValue(IdentityFieldEnum.Id), out var userId);

                if (parse)
                {
                    var cacheKey = CacheKeyTool.Get(CacheKeyEnum.User, userId);
                    if (!cache.TryGetValue(cacheKey, out _) || forceTC)
                    {
                        var user = await userService.Get(userId, [nameof(UserEntity.Status)]);
                        var userRole = context.User.FindFirstValue(IdentityFieldEnum.Role);
                        var settings = await settingService.GetCached();
                        var dateOfTC = userRole == UserRoleEnum.Customer ?
                                       settings.TermsAndConditionsContentDateCustomer :
                                       settings.TermsAndConditionsContentDateCompany;

                        DateTime.TryParse(context.User.FindFirstValue(IdentityFieldEnum.TermsAcceptDate), out var termsAcceptDate);
                        if (userRole != UserRoleEnum.Administrator && termsAcceptDate < dateOfTC)
                        {
                            context.Items["ShowTCModal"] = true;
                        }
                        else
                        {
                            forceTC = false;
                            cache.Set(cacheKey, true, TimeSpan.FromMinutes(ConstantEnum.UserCacheTimeout));
                        }
                        status = user?.Status;
                    }
                }

                if (!parse || userId == 0 || status != UserStatusEnum.Active)
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Account/Login");
                    return;
                }

                if (!forceTC && context.User.FindFirstValue(IdentityFieldEnum.TermsAccepted)?.ToLower() != "true")
                {
                    await context.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(
                            new ClaimsIdentity(
                                context.User.Claims
                                    .Where(c => c.Type != IdentityFieldEnum.TermsAccepted)
                                    .Append(new Claim(IdentityFieldEnum.TermsAccepted, "true")),
                                CookieAuthenticationDefaults.AuthenticationScheme
                            )
                        )
                    );
                }
            }

            await next(context);
        }
    }
}
