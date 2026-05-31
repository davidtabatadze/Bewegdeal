using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface ISettingsRepository : IRepository
    {
        Task<SettingsEntity> Get();
    }
}
