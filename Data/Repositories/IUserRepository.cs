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
        /// Returns all users matching all non-null criteria in
        /// <paramref name="filter"/>. Returns every user when the filter is empty.
        /// </summary>
        Task<List<UserEntity>> Load(UserFilter filter);

        /// <summary>
        /// Returns the count of users matching the non-null filter criteria,
        /// ignoring any paging (<c>Start</c> / <c>Length</c>).
        /// </summary>
        Task<int> Count(UserFilter filter);

        /// <summary>
        /// Inserts <paramref name="user"/> and returns the same object
        /// with the database-generated <c>Id</c> populated.
        /// </summary>
        Task<UserEntity> Create(UserEntity user);

        /// <summary>Updates only the <c>Status</c> column for the given user ID.</summary>
        Task SetUserStatus(long id, string status);

        /// <summary>Updates the <c>Password</c> and <c>Salt</c> columns for the given user ID.</summary>
        Task UpdatePassword(long id, string hash, string salt);
    }
}
