using Bewegdeal.Enums;
using Bewegdeal.Services;

namespace Bewegdeal.Middleware
{
    /// <summary>
    /// Restores the user session from the "bewegdeal_remember" persistent cookie
    /// when the session has expired but the cookie is still valid.
    /// </summary>
    public class RememberMeMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            // only act when session is empty and the remember-me cookie is present
            var userId = context.Session.GetString(ConstantEnum.SessionUserId);
            var cookie = context.Request.Cookies[ConstantEnum.CookieRemember];

            if (userId is null && !string.IsNullOrWhiteSpace(cookie) && long.TryParse(cookie, out var id))
            {
                var userService = context.RequestServices.GetRequiredService<UserService>();
                var user = await userService.GetUser(id);

                if (user is not null)
                {
                    context.Session.SetString(ConstantEnum.SessionUserId, user.Id.ToString());
                    context.Session.SetString(ConstantEnum.SessionUserRole, user.Role);
                    context.Session.SetString(ConstantEnum.SessionUserName, user.Name);

                    if (user.ProfilePictureFileId.HasValue)
                    {
                        context.Session.SetString(ConstantEnum.SessionUserPictureId, user.ProfilePictureFileId.Value.ToString());
                    }
                }
                else
                {
                    // cookie references a deleted/invalid user — clear it
                    context.Response.Cookies.Delete(ConstantEnum.CookieRemember);
                }
            }

            await next(context);
        }
    }
}
