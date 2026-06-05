using Bewegdeal.Data.Entities;
using Bewegdeal.Enums;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Bewegdeal.Tools
{
    public static class UserIdentityTool
    {
        public static ClaimsPrincipal BuildPrincipal(UserEntity user, string? avatarUrl = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Role, user.Role),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                // bewegdeal
                new(IdentityFieldEnum.Id, user.Id.ToString()),
                new(IdentityFieldEnum.Role, user.Role),
                new(IdentityFieldEnum.Name, user.Name),
                new(IdentityFieldEnum.Email, user.Email),
                new(IdentityFieldEnum.Theme, user.Theme),
                new(IdentityFieldEnum.AvatarUrl, avatarUrl ?? string.Empty),
                new(IdentityFieldEnum.AcquaintedHIW, user.AcquaintedHIW ? "true" : "false"),
                new(IdentityFieldEnum.TermsAcceptDate, user.TermsAndConditionsAcceptDate.ToString("o")),
                new(IdentityFieldEnum.TermsAccepted, "true")
            };

            return new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
            );
        }
    }
}
