using Bewegdeal.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bewegdeal.Filters
{
    /// <summary>
    /// Restricts access to administrators only.
    /// Unauthenticated requests are redirected to Login.
    /// Authenticated non-admin requests are redirected to the Dashboard.
    /// </summary>
    public class RequireAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            if (session.GetString(ConstantEnum.SessionUserId) is null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (session.GetString(ConstantEnum.SessionUserRole) != UserRoleEnum.Administrator)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}
