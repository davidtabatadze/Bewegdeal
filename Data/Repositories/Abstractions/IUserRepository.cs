using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IUserRepository : IRepository
    {
        Task<UserEntity> Create(UserEntity user);
        Task SetUserStatus(long id, string status);
        Task SetAcquaintedHIW(long id);
        Task UpdatePassword(long id, string hash, string salt);
        Task<UserEntity?> Get(UserFilter filter);
        Task<int> Count(UserFilter filter);
        Task<List<UserEntity>> Load(UserFilter filter);
    }
}
