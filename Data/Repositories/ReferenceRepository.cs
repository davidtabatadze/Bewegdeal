using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class ReferenceRepository(SqlContext context) : IReferenceRepository, IRepositorySeedable
    {
        public async Task Seed()
        {
            var rows = new[]
            {
                new ReferenceEntity { Id = UserRoleEnum.Administrator, Type = ReferenceTypeEnum.UserRole,   Name = "Administrator" },
                new ReferenceEntity { Id = UserRoleEnum.Customer,      Type = ReferenceTypeEnum.UserRole,   Name = "Customer"      },
                new ReferenceEntity { Id = UserRoleEnum.Company,       Type = ReferenceTypeEnum.UserRole,   Name = "Company"       },
                new ReferenceEntity { Id = UserStatusEnum.Active,      Type = ReferenceTypeEnum.UserStatus, Name = "Active"        },
                new ReferenceEntity { Id = UserStatusEnum.Pending,     Type = ReferenceTypeEnum.UserStatus, Name = "Pending"       },
                new ReferenceEntity { Id = UserStatusEnum.Blocked,     Type = ReferenceTypeEnum.UserStatus, Name = "Blocked"       },
                new ReferenceEntity { Id = UserStatusEnum.Unverified,  Type = ReferenceTypeEnum.UserStatus, Name = "Unverified" },
            };

            foreach (var row in rows)
            {
                if (await Get(row.Id) != null)
                {
                    continue;
                }

                await Create(row);
            }
        }

        // ── Write ────────────────────────────────────────────────────────────────

        public async Task<ReferenceEntity> Create(ReferenceEntity reference)
        {
            context.References.Add(reference);
            await context.SaveChangesAsync();
            return reference;
        }

        public async Task Update(ReferenceEntity reference)
        {
            context.References.Update(reference);
            await context.SaveChangesAsync();
        }

        // ── Read ─────────────────────────────────────────────────────────────────

        public async Task<ReferenceEntity?> Get(string id)
        {
            id ??= "-";
            return await context.References.FirstOrDefaultAsync(r => r.Id == id);
        }

        // ── Delete ───────────────────────────────────────────────────────────────
        // ***
    }
}
