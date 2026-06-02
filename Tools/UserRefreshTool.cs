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
                var forceTC = context.User.FindFirstValue("TermsAccepted") != "true";
                var parse = long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);

                if (parse)
                {
                    var cacheKey = CacheKeyTool.Get("bewegdeal_user", userId);
                    if (!cache.TryGetValue(cacheKey, out _) || forceTC)
                    {
                        var settings = await settingService.Get();
                        var user = await userService.Get(userId, [nameof(UserEntity.Status)]);

                        DateTime.TryParse(context.User.FindFirstValue("TermsAcceptDate"), out var termsAcceptDate);
                        if (
                            context.User.FindFirstValue(ClaimTypes.Role) != UserRoleEnum.Administrator &&
                            termsAcceptDate < settings.TermsAndConditionsContentDate
                        )
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

                if (!parse || status != UserStatusEnum.Active)
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Account/Login");
                    return;
                }

                if (!forceTC && context.User.FindFirstValue("TermsAccepted") != "true")
                {
                    await context.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(
                            new ClaimsIdentity(
                                context.User.Claims
                                    .Where(c => c.Type != "TermsAccepted")
                                    .Append(new Claim("TermsAccepted", "true")),
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
