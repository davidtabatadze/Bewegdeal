using Bewegdeal.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bewegdeal.Controllers
{
    public class XBaseController : Controller
    {
        public string BaseUrl => $"{Request.Scheme}://{Request.Host}";
        public long UserId => GetClaim<long>(IdentityFieldEnum.Id);
        public string UserRole => GetClaim<string>(IdentityFieldEnum.Role) ?? "undefined";

        protected T? GetClaim<T>(string type)
        {
            var value = User.FindFirstValue(type);
            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        protected bool HasClaim(string type, object value)
            => User.FindFirstValue(type)!.ToLower() == value.ToString()!.ToLower();

        protected async Task RefreshClaim(string type, object? value)
            => await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(
                    new ClaimsIdentity(
                        User.Claims
                            .Where(c => c.Type != type)
                            .Append(new Claim(type, value is null ? string.Empty : value.ToString()!)),
                        CookieAuthenticationDefaults.AuthenticationScheme
                    )
                )
            );
    }
}
