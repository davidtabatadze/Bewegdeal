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
        public async Task InvokeAsync(HttpContext context, UserService userService, IMemoryCache cache)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userStatus = UserStatusEnum.Active;
                var parse = long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);

                if (parse)
                {
                    var cacheKey = $"bewegdeal_user_{userId}";
                    if (!cache.TryGetValue(cacheKey, out _))
                    {
                        var user = await userService.Get(userId, [nameof(UserEntity.Status)]);
                        userStatus = user?.Status;
                        cache.Set(cacheKey, true, TimeSpan.FromMinutes(ConstantEnum.UserCacheTimeout));
                    }
                }

                if (!parse || userStatus != UserStatusEnum.Active)
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }

            await next(context);
        }
    }
}
