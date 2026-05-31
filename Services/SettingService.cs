using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;

namespace Bewegdeal.Services
{
    public class SettingService(ISettingsRepository SettingRepository, FileService FileService)
    {

        public async Task<SettingsEntity> Get()
            => await SettingRepository.Get();

        public async Task<string?> GetTermsAndConditionsUrl()
            => await FileService.GetUrl((await Get()).TermsAndConditionsFileId);

    }
}
