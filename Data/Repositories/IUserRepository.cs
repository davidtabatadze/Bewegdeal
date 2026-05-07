using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;

namespace Bewegdeal.Data.Repositories
{
    /// <summary>
    /// Pure data-access contract for the Users table.
    /// No business logic — only reads and writes.
    /// </summary>
    public interface IUserRepository : IRepository
    {
        /// <summary>
        /// Returns the first user matching all non-null criteria in
        /// <paramref name="filter"/>, or null if none found.
        /// </summary>
        Task<UserEntity?> Get(UserFilter filter);

        /// <summary>
        /// Inserts <paramref name="user"/> and returns the same object
        /// with the database-generated <c>Id</c> populated.
        /// </summary>
        Task<UserEntity> Create(UserEntity user);

        /// <summary>Persists changes to an existing user row.</summary>
        Task Update(UserEntity user);
    }
}
