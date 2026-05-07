using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Tools;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="IUserRepository"/>.
    /// Scoped per request. Interacts with the database only — no business logic.
    /// </summary>
    public class UserRepository(SqlContext context) : IUserRepository, IRepositorySeedable
    {
        private readonly SqlContext _context = context;

        public async Task Seed()
        {
            var rows = new[]
            {
                new UserEntity { Id = 1, Name = "Administrator",   Email = "admin@bewegdeal.at",          Mobile = "+995599438038",  Password = "asdAsd123" },
                new UserEntity { Id = 2, Name = "David Tabatadze", Email = "david.tabatadze@outlook.com", Mobile = "+4369910433340", Password = "asdAsd123" },
            };

            foreach (var row in rows)
            {
                if (await Get(new UserFilter { Id = row.Id }) != null) continue;

                var (hash, salt) = PasswordTool.HashPassword(row.Password);

                await Create(new UserEntity
                {
                    Id = row.Id,
                    Code = row.Id.ToString(),
                    Role = UserRoleEnum.Administrator,
                    Status = UserStatusEnum.Active,
                    Name = row.Name,
                    Email = row.Email,
                    Mobile = row.Mobile,
                    Address = row.Address,
                    Password = hash,
                    Salt = salt,
                    Interests = [ServiceEnum.All],
                });
            }
        }

        // ── Read ─────────────────────────────────────────────────────────────────

        public async Task<UserEntity?> Get(UserFilter filter)
        {
            var query = _context.Users.AsQueryable();

            if (filter.Id.HasValue)
                query = query.Where(u => u.Id == filter.Id.Value);

            if (filter.Email is not null)
            {
                var lower = filter.Email.ToLower();
                query = query.Where(u => u.Email.ToLower() == lower);
            }

            return await query.FirstOrDefaultAsync();
        }

        // ── Write ────────────────────────────────────────────────────────────────

        public async Task<UserEntity> Create(UserEntity user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task Update(UserEntity user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
