using Bewegdeal.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bewegdeal.Filters
{
    /// <summary>
    /// Redirects unauthenticated requests to the Login page.
    /// Apply to any controller or action that requires a logged-in user.
    /// </summary>
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetString(ConstantEnum.SessionUserId);
            if (userId is null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }
}
