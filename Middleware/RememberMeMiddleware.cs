using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;

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
                // resolve scoped repository within this request scope
                var userRepository = context.RequestServices.GetRequiredService<IUserRepository>();
                var user = await userRepository.Get(new UserFilter { Id = id });

                if (user is not null)
                {
                    context.Session.SetString(ConstantEnum.SessionUserId, user.Id.ToString());
                    context.Session.SetString(ConstantEnum.SessionUserRole, user.Role);
                    context.Session.SetString(ConstantEnum.SessionUserName, user.Name);

                    if (user.ProfilePictureFileId.HasValue)
                    {
                        var fileRepository = context.RequestServices.GetRequiredService<IFileRepository>();
                        var pictureFile = await fileRepository.Get(user.ProfilePictureFileId.Value);
                        if (pictureFile is not null)
                        {
                            context.Session.SetString(ConstantEnum.SessionUserPictureKey, pictureFile.Key);
                        }
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
