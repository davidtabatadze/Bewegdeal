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
                new UserEntity { Id = 1, Name = "Administrator",   Email = "admin@bewegdeal.at",           Password = "asdASD123", Role = UserRoleEnum.Administrator },
                new UserEntity { Id = 2, Name = "Datiko Admin",    Email = "datiko.admin@bewegdeal.at",    Password = "asdASD123", Role = UserRoleEnum.Administrator },
                new UserEntity { Id = 3, Name = "Datiko Customer", Email = "datiko.customer@bewegdeal.at", Password = "asdASD123", Role = UserRoleEnum.Customer },
                new UserEntity { Id = 4, Name = "Datiko Company",  Email = "datiko.company@bewegdeal.at",  Password = "asdASD123", Role = UserRoleEnum.Company },
                new UserEntity { Id = 5, Name = "Gio Admin",       Email = "gio.admin@bewegdeal.at",       Password = "asdASD123", Role = UserRoleEnum.Administrator },
                new UserEntity { Id = 6, Name = "Gio Customer",    Email = "gio.customer@bewegdeal.at",    Password = "asdASD123", Role = UserRoleEnum.Customer },
                new UserEntity { Id = 7, Name = "Gio Company",     Email = "gio.company@bewegdeal.at",     Password = "asdASD123", Role = UserRoleEnum.Company },

                //new UserEntity { Id = 8, Name = "Gerhard Schröder",Email = "gerhard@bewegdeal.at", Password = "asdASD123", Role = UserRoleEnum.Customer },
                //new UserEntity { Id = 9, Name = "Bastian Schweinsteiger",Email = "bastian@bewegdeal.at", Password = "asdASD123", Role = UserRoleEnum.Customer },
                //new UserEntity { Id = 10, Name = "Ludwig Van Beethoven",Email = "ludwig@bewegdeal.at", Password = "asdASD123", Role = UserRoleEnum.Customer },
                //new UserEntity { Id = 11, Name = "Mercedes Benz",Email = "benz@bewegdeal.at", Password = "asdASD123", Role = UserRoleEnum.Company, Number = "000", Address="000" },
                //new UserEntity { Id = 12, Name = "Bayern Motorische Werke",Email = "bmw@bewegdeal.at", Password = "asdASD123", Role = UserRoleEnum.Company, Number = "111", Address="111" },
                //new UserEntity { Id = 13, Name = "Über Alles",Email = "uber@bewegdeal.at", Password = "asdASD123", Role = UserRoleEnum.Company, Number = "222", Address="222" },
            };

            foreach (var row in rows)
            {
                if (await Get<UserEntity>(row.Id) != null)
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
                    Number = row.Number ?? "-",
                    Mobile = row.Mobile ?? "-",
                    Address = row.Address,
                    Password = hash,
                    Salt = salt,
                    CreateDate = DateTime.Now,
                    TermsAndConditionsAcceptDate = DateTime.Now
                });
            }
        }

        public async Task Rate(long userId, long evaluatorId, decimal value)
        {
            if (value == 0)
            {
                return;
            }

            await Create(new UserRatingEntity
            {
                Value = value,
                UserId = userId,
                EvaluatorId = evaluatorId,
                CreateDate = DateTime.Now
            });

            var average = await Context.UserRatings
                .Where(r => r.UserId == userId)
                .AverageAsync(r => (double)r.Value);

            var rating = (decimal)(Math.Ceiling(average * 2) / 2);

            await Update(UserUpdateAreaEnum.Rating, new UserEntity { Id = userId, Rating = rating });
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

                case UserUpdateAreaEnum.AcceptTerms:
                    await Context.Users.Where(u => u.Id == update.Id)
                                       .ExecuteUpdateAsync(u =>
                                            u.SetProperty(p => p.TermsAndConditionsAcceptDate, DateTime.Now)
                                       );
                    break;

                case UserUpdateAreaEnum.AcceptHIW:
                    await Context.Users.Where(u => u.Id == update.Id)
                                       .ExecuteUpdateAsync(u =>
                                            u.SetProperty(p => p.AcquaintedHIW, true)
                                       );
                    break;

                case UserUpdateAreaEnum.Theme:
                    await Context.Users.Where(u => u.Id == update.Id)
                                       .ExecuteUpdateAsync(u =>
                                            u.SetProperty(p => p.Theme, update.Theme)
                                       );
                    break;

                case UserUpdateAreaEnum.Avatar:
                    await Context.Users.Where(u => u.Id == update.Id)
                                       .ExecuteUpdateAsync(u =>
                                            u.SetProperty(p => p.Avatar, update.Avatar)
                                       );
                    break;

                case UserUpdateAreaEnum.Rating:
                    await Context.Users.Where(u => u.Id == update.Id)
                                       .ExecuteUpdateAsync(u =>
                                            u.SetProperty(p => p.Rating, update.Rating)
                                       );
                    break;

                case UserUpdateAreaEnum.Profile:
                    await Context.Users.Where(u => u.Id == update.Id)
                                       .ExecuteUpdateAsync(u => u
                                            .SetProperty(p => p.Name, update.Name)
                                            .SetProperty(p => p.Address, update.Address)
                                            .SetProperty(p => p.Interests, update.Interests)
                                            .SetProperty(p => p.ServiceTerms, update.ServiceTerms)
                                       );
                    break;

                default:
                    throw new ArgumentException("Invalid update area", nameof(area));
            }
        }

        public async Task<UserEntity?> GetRegistered(string email, string mobile)
            => Context.Users.Where(u =>
                                u.Email.ToLower() == (email ?? "-").ToLower() ||
                                u.Mobile.ToLower() == (mobile ?? "-").ToLower()
                            )
                            .Select(BuildSelect<UserEntity>([nameof(UserEntity.Id)]))
                            .FirstOrDefault();

        public async Task<UserEntity?> Get(UserFilter filter, string[]? properties = null)
            => await ApplyFilters(Context.Users.AsQueryable(), filter).Select(BuildSelect<UserEntity>(properties)).FirstOrDefaultAsync();

        public async Task<List<UserEntity>> Load(UserFilter filter, string[]? properties = null)
            => await ApplyFilters(Context.Users.AsQueryable(), filter).Select(BuildSelect<UserEntity>(properties)).ToListAsync();

        public async Task<int> Count(UserFilter filter)
            => await ApplyFilters(Context.Users.AsQueryable(), filter).CountAsync();

        private IQueryable<UserEntity> ApplyFilters(IQueryable<UserEntity> query, UserFilter filter)
        {
            if (filter.Id.HasValue)
            {
                query = query.Where(u => u.Id == filter.Id.Value);
            }

            if (filter.DateFrom.HasValue)
            {
                query = query.Where(u => u.CreateDate >= filter.DateFrom.Value);
            }

            if (filter.DateTo.HasValue)
            {
                query = query.Where(u => u.CreateDate <= filter.DateTo.Value);
            }

            if (filter.Ids != null && filter.Ids.Count != 0)
            {
                query = query.Where(u => filter.Ids.Contains(u.Id));
            }

            if (!string.IsNullOrWhiteSpace(filter.Email))
            {
                query = query.Where(u => u.Email.ToLower() == filter.Email.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(u => u.Status == filter.Status);
            }

            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                query = query.Where(u => u.Role == filter.Role);
            }

            if (!string.IsNullOrWhiteSpace(filter.ExcludeRole))
            {
                query = query.Where(u => u.Role != filter.ExcludeRole);
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
