using Bewegdeal.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class XBaseController : Controller
    {
        public string BaseUrl => $"{Request.Scheme}://{Request.Host}";
        public string? UserRole => HttpContext.Session.GetString(ConstantEnum.SessionUserRole)?.Trim();
        public long? UserId => long.TryParse(HttpContext.Session.GetString(ConstantEnum.SessionUserId), out var id) ? id : null;
    }
}
