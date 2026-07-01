using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Tools;
using Microsoft.Extensions.Caching.Memory;

namespace Bewegdeal.Services
{
    public class SettingService(ISettingsRepository SettingRepository, IMemoryCache Cache)
    {
        public async Task<SettingsEntity> Get()
            => await SettingRepository.Get();
        private async Task Update(SettingsEntity settings)
            => await SettingRepository.Update(settings);
        private void ClearCache()
            => Cache.Remove(CacheKeyTool.Get(CacheKeyEnum.Settings));

        public async Task<SettingsEntity> GetCached()
        {
            var key = CacheKeyTool.Get(CacheKeyEnum.Settings);
            var settings = Cache.Get<SettingsEntity>(key);

            if (settings is not null)
            {
                return settings;
            }

            settings = await Get();
            Cache.Set(key, settings);
            return settings;
        }

        public async Task SaveAboutUs(string? content)
        {
            var settings = await Get();
            settings.AboutUs = content ?? string.Empty;
            await Update(settings);
            ClearCache();
        }

        public async Task SavePrivacyPolicy(string? content)
        {
            var settings = await Get();
            settings.PrivacyPolicy = content ?? string.Empty;
            await Update(settings);
            ClearCache();
        }

        public async Task SaveTermsAndConditionsCustomer(string? content)
        {
            var settings = await Get();
            settings.TermsAndConditionsContentCustomer = content ?? string.Empty;
            settings.TermsAndConditionsContentDateCustomer = DateTime.Now;
            await Update(settings);
            ClearCache();
        }

        public async Task SaveTermsAndConditionsCompany(string? content)
        {
            var settings = await Get();
            settings.TermsAndConditionsContentCompany = content ?? string.Empty;
            settings.TermsAndConditionsContentDateCompany = DateTime.Now;
            await Update(settings);
            ClearCache();
        }

        public async Task SaveInvoice(short commissionPersent, short taxPersent, short dueDays)
        {
            var settings = await Get();

            settings.InvoiceCommissionPersent = commissionPersent;
            settings.InvoiceTaxPersent = taxPersent;
            settings.InvoiceDueDays = dueDays;

            await Update(settings);
            ClearCache();
        }

        public async Task SaveMobile(string? mobilePrefix)
        {
            var settings = await Get();

            settings.MobilePrefix = mobilePrefix ?? "";

            await Update(settings);
            ClearCache();
        }

        public async Task SaveRequest(short imageMaxCount, short imageMaxSize, short videoMaxCount, short videoMaxSize)
        {
            var settings = await Get();

            settings.RequestImageMaxCount = imageMaxCount;
            settings.RequestImageMaxSize = imageMaxSize;
            settings.RequestVideoMaxCount = videoMaxCount;
            settings.RequestVideoMaxSize = videoMaxSize;

            await Update(settings);
            ClearCache();
        }

    }
}
