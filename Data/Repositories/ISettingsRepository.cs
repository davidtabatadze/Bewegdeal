using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;

namespace Bewegdeal.Data.Repositories
{
    public interface ISettingsRepository : IRepository
    {
        /// <summary>Returns the single settings row. Always succeeds — the row is guaranteed by seeding.</summary>
        Task<SettingsEntity> Get();

        /// <summary>Persists all settings fields for the given entity.</summary>
        Task Update(SettingsEntity settings);
    }
}
