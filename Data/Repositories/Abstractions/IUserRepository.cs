using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IUserRepository : IRepository
    {
        Task Update(UserUpdateAreaEnum area, UserEntity update);
        Task<UserEntity?> Get(UserFilter filter, string[]? properties = null);
        Task<UserEntity?> GetRegistered(string email, string mobile);
        Task<int> Count(UserFilter filter);
        Task<List<UserEntity>> Load(UserFilter filter);
    }
}
