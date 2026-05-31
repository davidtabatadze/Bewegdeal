using System.Security.Claims;
using Bewegdeal.Enums;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Bewegdeal.Middleware
{
    public class StatusRefreshMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, UserService userService, IMemoryCache cache)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                if (long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                {
                    var cacheKey = $"bewegdeal_user_{userId}";
                    if (!cache.TryGetValue(cacheKey, out _))
                    {
                        var user = await userService.Get(userId);
                        if (user?.Status != UserStatusEnum.Active)
                        {
                            context.Session.Clear();
                            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            context.Response.Redirect("/Account/Login");
                            return;
                        }
                        cache.Set(cacheKey, true, TimeSpan.FromMinutes(ConstantEnum.UserCacheTimeout));
                    }
                }
            }

            await next(context);
        }
    }
}
