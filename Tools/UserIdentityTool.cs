using Bewegdeal.Data.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Bewegdeal.Tools
{
    public static class UserIdentityTool
    {
        public static ClaimsPrincipal BuildPrincipal(UserEntity user, string? pictureUrl = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Role, user.Role),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                new("Theme", user.Theme),
                new("PictureUrl", pictureUrl ?? string.Empty),
                new("AcquaintedHIW", user.AcquaintedHIW ? "true" : "false"),
                new("TermsAcceptDate", user.TermsAndConditionsAcceptDate.ToString("o")),
            };

            return new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
            );
        }
    }
}
