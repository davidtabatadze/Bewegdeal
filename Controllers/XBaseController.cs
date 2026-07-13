using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Bewegdeal.Controllers
{
    public class XBaseController : Controller
    {
        public string BaseUrl => $"{Request.Scheme}://{Request.Host}";
        public long UserId => GetClaim<long>(IdentityFieldEnum.Id);
        public string UserRole => GetClaim<string>(IdentityFieldEnum.Role) ?? "undefined";

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {

            if (!HttpContext.Request.Headers.ContainsKey("X-Requested-With"))
            {
                if (User.IsInRole(UserRoleEnum.Administrator))
                {
                    var chatService = HttpContext.RequestServices.GetRequiredService<ChatService>();
                    var userService = HttpContext.RequestServices.GetRequiredService<UserService>();

                    var dubiousCount = await chatService.Count(new ChatFilter { Fraud = ChatFraudEnum.Dubious });
                    var pendingCount = await userService.Count(new UserFilter { Status = UserStatusEnum.Pending });

                    ViewBag.DubiousChatCount = dubiousCount;
                    ViewBag.PendingUserCount = pendingCount;
                }
                if (User.IsInRole(UserRoleEnum.Administrator) || User.IsInRole(UserRoleEnum.Company))
                {
                    var invoiceService = HttpContext.RequestServices.GetRequiredService<InvoiceService>();

                    var pendingCount = await invoiceService.Count(new InvoiceFilter
                    {
                        ViewerId = UserId,
                        ViewerRole = UserRole,
                        Status = InvoiceStatusEnum.Pending
                    });

                    ViewBag.PendingInvoiceCount = pendingCount;
                }
            }

            await next();
        }

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
        {
            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    User.Claims
                        .Where(c => c.Type != type)
                        .Append(new Claim(type, value is null ? string.Empty : value.ToString()!)),
                    CookieAuthenticationDefaults.AuthenticationScheme
                )
            );
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            HttpContext.User = principal;
        }
    }
}
