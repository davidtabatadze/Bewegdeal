using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="IReferenceRepository"/>.
    /// Scoped per request. Interacts with the database only — no business logic.
    /// </summary>
    public class ReferenceRepository(SqlContext context) : IReferenceRepository, IRepositorySeedable
    {
        private readonly SqlContext _context = context;

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
                if (await Get(new BaseFilter<string> { Id = row.Id }) != null)
                {
                    continue;
                }

                await Create(row);
            }
        }

        // ── Read ─────────────────────────────────────────────────────────────────

        public async Task<ReferenceEntity?> Get(BaseFilter<string> filter)
        {
            var query = _context.References.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Id))
            {
                query = query.Where(r => r.Id == filter.Id);
            }

            return await query.FirstOrDefaultAsync();
        }

        // ── Write ────────────────────────────────────────────────────────────────

        public async Task<ReferenceEntity> Create(ReferenceEntity reference)
        {
            _context.References.Add(reference);
            await _context.SaveChangesAsync();
            return reference;
        }

        public async Task Update(ReferenceEntity reference)
        {
            _context.References.Update(reference);
            await _context.SaveChangesAsync();
        }
    }
}
