using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;

namespace Bewegdeal.Data.Repositories
{
    /// <summary>
    /// Pure data-access contract for the References table.
    /// No business logic — only reads and writes.
    /// </summary>
    public interface IReferenceRepository : IRepository
    {
        /// <summary>
        /// Returns the first reference matching all non-null criteria in
        /// <paramref name="filter"/>, or null if none found.
        /// </summary>
        Task<ReferenceEntity?> Get(BaseFilter<string> filter);

        /// <summary>
        /// Inserts <paramref name="reference"/> and returns the same object.
        /// </summary>
        Task<ReferenceEntity> Create(ReferenceEntity reference);

        /// <summary>Persists changes to an existing reference row.</summary>
        Task Update(ReferenceEntity reference);
    }
}
