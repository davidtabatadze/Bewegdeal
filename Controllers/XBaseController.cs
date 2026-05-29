using Bewegdeal.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class XBaseController : Controller
    {
        public string BaseUrl => $"{Request.Scheme}://{Request.Host}";
        public string? UserId => HttpContext.Session.GetString(ConstantEnum.SessionUserId)?.Trim();
        public string? UserRole => HttpContext.Session.GetString(ConstantEnum.SessionUserRole)?.Trim();
        public string? UserEmail => HttpContext.Session.GetString(ConstantEnum.SessionUserEmail)?.Trim();
    }
}
