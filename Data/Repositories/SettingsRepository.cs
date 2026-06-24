using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class SettingsRepository(SqlContext SqlContext) : BaseRepository(SqlContext), ISettingsRepository, IRepositorySeedable
    {

        public async Task Seed()
        {
            if (!await Context.Settings.AnyAsync())
            {
                await Create(new SettingsEntity
                {
                    Id = 1,
                    TermsAndConditionsContentCustomer = "This is a free T&C for customers",
                    TermsAndConditionsContentCompany = "This is a free T&C for companies",
                    TermsAndConditionsContentDateCustomer = DateTime.Now,
                    TermsAndConditionsContentDateCompany = DateTime.Now,
                    RequestImageMaxCount = 5,
                    RequestImageMaxSize = 4,
                    RequestVideoMaxCount = 1,
                    RequestVideoMaxSize = 20,
                    RequestNegotiationMinutes = 60,
                    InvoiceCommissionPersent = 10,
                    InvoiceTaxPersent = 20,
                    MobilePrefix = "+43"
                });
            }
        }

        public async Task<SettingsEntity> Get()
        {
            return await Get<SettingsEntity>(1) ?? new SettingsEntity { Id = 1 };
        }

    }
}