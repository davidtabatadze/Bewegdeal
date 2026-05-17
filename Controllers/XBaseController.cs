using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    public class XBaseController(IUserRepository userRepository) : Controller
    {
        protected async Task<UserEntity?> GetUser(
            List<string>? roles = null,
            bool? active = null,
            bool? hiw = null
        )
        {
            if (!long.TryParse(HttpContext.Session.GetString("UserId"), out var id))
            {
                return null;
            }

            var user = await userRepository.Get(new UserFilter { Id = id });

            if (user is null)
            {
                return null;
            }

            if (roles is not null && !roles.Contains(user.Role))
            {
                return null;
            }

            if (hiw is not null && user.AcquaintedHIW != hiw)
            {
                return null;
            }

            if (active is not null && user.Status != UserStatusEnum.Active)
            {
                return null;
            }

            return user;
        }
    }
}
