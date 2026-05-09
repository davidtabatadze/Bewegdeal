using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories;

namespace Bewegdeal.Middleware
{
    /// <summary>
    /// Restores the user session from the "bewegdeal_remember" persistent cookie
    /// when the session has expired but the cookie is still valid.
    /// </summary>
    public class RememberMeMiddleware(RequestDelegate next)
    {
        private const string CookieName = "bewegdeal_remember";

        public async Task InvokeAsync(HttpContext context)
        {
            // only act when session is empty and the remember-me cookie is present
            var userId = context.Session.GetString("UserId");
            var cookie = context.Request.Cookies[CookieName];

            if (userId is null && !string.IsNullOrWhiteSpace(cookie) && long.TryParse(cookie, out var id))
            {
                // resolve scoped repository within this request scope
                var userRepository = context.RequestServices.GetRequiredService<IUserRepository>();
                var user = await userRepository.Get(new UserFilter { Id = id });

                if (user is not null)
                {
                    context.Session.SetString("UserId", user.Id.ToString());
                    context.Session.SetString("UserRole", user.Role);
                    context.Session.SetString("UserName", user.Name);
                }
                else
                {
                    // cookie references a deleted/invalid user — clear it
                    context.Response.Cookies.Delete(CookieName);
                }
            }

            await next(context);
        }
    }
}
