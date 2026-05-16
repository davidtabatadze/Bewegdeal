using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Tools;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class UserRepository(SqlContext context) : IUserRepository, IRepositorySeedable
    {

        public async Task Seed()
        {
            var rows = new[]
            {
                new UserEntity { Id = 1, Name = "Administrator",   Email = "admin@bewegdeal.at",           Mobile = "+4369910433340", Password = "asdASD123", Role = UserRoleEnum.Administrator },
                new UserEntity { Id = 2, Name = "Datiko Admin",    Email = "datiko.admin@bewegdeal.at",    Mobile = "+995599438038",  Password = "asdASD123", Role = UserRoleEnum.Administrator },
                new UserEntity { Id = 3, Name = "Datiko Customer", Email = "datiko.customer@bewegdeal.at", Mobile = "+995599438038",  Password = "asdASD123", Role = UserRoleEnum.Customer },
                new UserEntity { Id = 4, Name = "Datiko Company",  Email = "datiko.company@bewegdeal.at",  Mobile = "+995599438038",  Password = "asdASD123", Role = UserRoleEnum.Company },
            };

            foreach (var row in rows)
            {
                if (await Get(new UserFilter { Id = row.Id }) != null)
                {
                    continue;
                }

                var (hash, salt) = PasswordTool.HashPassword(row.Password);

                await Create(new UserEntity
                {
                    Id = row.Id,
                    Role = row.Role,
                    Status = UserStatusEnum.Active,
                    Name = row.Name,
                    Email = row.Email,
                    Mobile = row.Mobile,
                    Address = row.Address,
                    Password = hash,
                    Salt = salt
                });
            }
        }

        // ── Write ────────────────────────────────────────────────────────────────

        public async Task<UserEntity> Create(UserEntity user)
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        public async Task SetUserStatus(long id, string status)
        {
            await context.Users
                .Where(u => u.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, status));
        }

        public async Task UpdatePassword(long id, string hash, string salt)
        {
            await context.Users
                .Where(u => u.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.Password, hash)
                    .SetProperty(u => u.Salt, salt)
                );
        }

        // ── Read ─────────────────────────────────────────────────────────────────

        public async Task<UserEntity?> Get(UserFilter filter)
        {
            var query = context.Users.AsQueryable();

            if (filter.Id.HasValue)
            {
                query = query.Where(u => u.Id == filter.Id.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Email))
            {
                query = query.Where(u => u.Email.ToLower() == filter.Email.ToLower());
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<int> Count(UserFilter filter)
        {
            return await ApplyFilters(context.Users.AsQueryable(), filter).CountAsync();
        }

        public async Task<List<UserEntity>> Load(UserFilter filter)
        {
            var query = ApplyFilters(context.Users.AsQueryable(), filter);

            if (!string.IsNullOrWhiteSpace(filter.SortDirection) && !string.IsNullOrWhiteSpace(filter.SortField))
            {
                var desc = filter.SortDirection == SortDirectionEnum.Desc;
                query = filter.SortField switch
                {
                    SortFieldEnum.Status => desc ? query.OrderByDescending(u => u.Status) : query.OrderBy(u => u.Status),
                    _ => desc ? query.OrderByDescending(u => u.Id) : query.OrderBy(u => u.Id)
                };
            }

            if (filter.Start.HasValue && filter.Length.HasValue)
            {
                query = query.Skip(filter.Start.Value);
                query = query.Take(filter.Length.Value);
            }

            return await query.ToListAsync();
        }

        private static IQueryable<UserEntity> ApplyFilters(IQueryable<UserEntity> query, UserFilter filter)
        {
            if (filter.Id.HasValue)
            {
                query = query.Where(u => u.Id == filter.Id.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Email))
            {
                query = query.Where(u => u.Email.ToLower() == filter.Email.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                query = query.Where(u => u.Role == filter.Role);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(u => u.Status == filter.Status);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term) ||
                    (u.Mobile != null && u.Mobile.ToLower().Contains(term)) ||
                    (u.Address != null && u.Address.ToLower().Contains(term))
                );
            }

            return query;
        }

        // ── Delete ───────────────────────────────────────────────────────────────
        // ***
    }
}
