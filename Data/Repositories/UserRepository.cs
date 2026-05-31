using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Tools;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class UserRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IUserRepository, IRepositorySeedable
    {

        public async Task Seed()
        {
            var rows = new[]
            {
                new UserEntity { Id = 1, Name = "Administrator",   Email = "admin@bewegdeal.at",           Mobile = "+4369910433340", Password = "asdASD123", Role = UserRoleEnum.Administrator },
                new UserEntity { Id = 2, Name = "Datiko Admin",    Email = "datiko.admin@bewegdeal.at",    Mobile = "+995599438038",  Password = "asdASD123", Role = UserRoleEnum.Administrator },
                new UserEntity { Id = 3, Name = "Datiko Customer", Email = "datiko.customer@bewegdeal.at", Mobile = "+995599438038",  Password = "asdASD123", Role = UserRoleEnum.Customer },
                new UserEntity { Id = 4, Name = "Datiko Company",  Email = "datiko.company@bewegdeal.at",  Mobile = "+995599438038",  Password = "asdASD123", Role = UserRoleEnum.Company },
                new UserEntity { Id = 5, Name = "Gio Admin",       Email = "gio.admin@bewegdeal.at",       Mobile = "+995555944072",  Password = "asdASD123", Role = UserRoleEnum.Administrator },
                new UserEntity { Id = 6, Name = "Gio Customer",    Email = "gio.customer@bewegdeal.at",    Mobile = "+995555944072",  Password = "asdASD123", Role = UserRoleEnum.Customer },
                new UserEntity { Id = 7, Name = "Gio Company",     Email = "gio.company@bewegdeal.at",     Mobile = "+995555944072",  Password = "asdASD123", Role = UserRoleEnum.Company },
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

        public async Task Update(UserUpdateAreaEnum area, UserEntity update)
        {
            switch (area)
            {

                case UserUpdateAreaEnum.Status:
                    await Context.Users.Where(u => u.Id == update.Id)
                                       .ExecuteUpdateAsync(u =>
                                            u.SetProperty(p => p.Status, update.Status)
                                       );
                    break;

                case UserUpdateAreaEnum.Password:
                    await Context.Users.Where(u => u.Id == update.Id)
                                       .ExecuteUpdateAsync(u => u
                                           .SetProperty(p => p.Password, update.Password)
                                           .SetProperty(p => p.Salt, update.Salt)
                                       );
                    break;

                default:
                    throw new ArgumentException("Invalid update area", nameof(area));
            }
        }

        public async Task<UserEntity?> Get(UserFilter filter, string[]? properties = null)
            => await ApplyFilters(Context.Users.AsQueryable(), filter).Select(BuildSelect<UserEntity>(properties)).FirstOrDefaultAsync();

        public async Task<List<UserEntity>> Load(UserFilter filter)
            => await ApplyFilters(Context.Users.AsQueryable(), filter).ToListAsync();

        public async Task<int> Count(UserFilter filter)
            => await ApplyFilters(Context.Users.AsQueryable(), filter).CountAsync();

        private IQueryable<UserEntity> ApplyFilters(IQueryable<UserEntity> query, UserFilter filter)
        {
            if (filter.Id.HasValue)
            {
                query = query.Where(u => u.Id == filter.Id.Value);
            }

            if (filter.Ids != null && filter.Ids.Count != 0)
            {
                query = query.Where(u => filter.Ids.Contains(u.Id));
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

            query = ApplySorting(query, filter);
            query = ApplyPaging(query, filter);


            return query;
        }

    }
}
