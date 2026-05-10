using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class SettingsRepository(SqlContext context) : ISettingsRepository, IRepositorySeedable
    {
        public async Task Seed()
        {
            if (!await context.Settings.AnyAsync())
            {
                await context.Settings.AddAsync(new SettingsEntity
                {
                    Id = 1,
                    RequestImageMaxCount = 5,
                    RequestImageMaxSize = 4,
                    RequestVideoMaxCount = 1,
                    RequestVideoMaxSize = 20,
                    RequestNegotiationMinutes = 60,
                    TermsAndConditionsFileId = 1
                });
                await context.SaveChangesAsync();
            }
        }

        public async Task<SettingsEntity> Get()
        {
            // There is always exactly one row after seeding.
            // The fallback with Id = 1 is a safety net for unexpected states.
            return await context.Settings.FirstOrDefaultAsync()
                ?? new SettingsEntity { Id = 1 };
        }

        public async Task Update(SettingsEntity settings)
        {
            await context.Settings
                .Where(s => s.Id == settings.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.TermsAndConditionsFileId, settings.TermsAndConditionsFileId)
                    .SetProperty(p => p.RequestNegotiationMinutes, settings.RequestNegotiationMinutes)
                    .SetProperty(p => p.RequestImageMaxCount, settings.RequestImageMaxCount)
                    .SetProperty(p => p.RequestImageMaxSize, settings.RequestImageMaxSize)
                    .SetProperty(p => p.RequestVideoMaxCount, settings.RequestVideoMaxCount)
                    .SetProperty(p => p.RequestVideoMaxSize, settings.RequestVideoMaxSize)
                );
        }
    }
}
