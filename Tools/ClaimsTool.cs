using System.Security.Claims;
using Bewegdeal.Data.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Bewegdeal.Tools
{
    public static class ClaimsTool
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
            };

            return new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
            );
        }
    }
}
