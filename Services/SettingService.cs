using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;

namespace Bewegdeal.Services
{
    public class SettingService(ISettingsRepository SettingRepository)
    {
        public async Task<SettingsEntity> Get()
            => await SettingRepository.Get();
    }
}
