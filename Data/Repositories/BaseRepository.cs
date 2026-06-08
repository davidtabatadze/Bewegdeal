using Bewegdeal.Data.Base;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Bewegdeal.Data.Repositories
{
    public class BaseRepository(SqlContext SqlContext)
    {

        protected SqlContext Context { get; set; } = SqlContext;

        public async Task<T> Create<T>(T entity) where T : class, IEntity
        {
            await Context.Set<T>().AddAsync(entity);
            await Context.SaveChangesAsync();
            return entity;
        }

        public async Task Create<T>(IEnumerable<T> entities) where T : class, IEntity
        {
            await Context.Set<T>().AddRangeAsync(entities);
            await Context.SaveChangesAsync();
        }

        public async Task Update<T>(T entity) where T : class, IEntity
        {
            Context.Set<T>().Update(entity);
            await Context.SaveChangesAsync();
        }

        public async Task Delete<T>(long id) where T : class, IEntity
            => await Context.Set<T>().Where(f => f.Id == id).ExecuteDeleteAsync();

        public async Task Delete<T>(List<long> ids) where T : class, IEntity
        {
            await Context.RequestFiles
                         .Where(i => ids.Contains(i.Id))
                         .ExecuteDeleteAsync();
        }

        public async Task<T?> Get<T>(long id, string[]? properties = null) where T : class, IEntity
            => await Context.Set<T>().Where(i => i.Id == id).Select(BuildSelect<T>(properties)).FirstOrDefaultAsync();

        public Expression<Func<T, T>> BuildSelect<T>(string[]? properties) where T : class, IEntity
        {
            if (properties is null || properties.Length == 0)
            {
                return x => x;
            }

            var param = Expression.Parameter(typeof(T), "x");

            var bindings = properties.Select(name =>
            {
                var prop = typeof(T).GetProperty(name)!;
                return Expression.Bind(prop, Expression.Property(param, prop));
            });

            var body = Expression.MemberInit(Expression.New(typeof(T)), bindings);

            return Expression.Lambda<Func<T, T>>(body, param);
        }

        public IQueryable<T> ApplySorting<T>(IQueryable<T> query, BaseFilter? filter) where T : class, IEntity
        {
            if (!string.IsNullOrWhiteSpace(filter?.SortDirection) && !string.IsNullOrWhiteSpace(filter?.SortField))
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(parameter, filter.SortField);
                var lambda = Expression.Lambda(property, parameter);
                var method = filter.SortDirection == SortDirectionEnum.Desc ? "OrderByDescending" : "OrderBy";

                var result = typeof(Queryable).GetMethods()
                    .First(m => m.Name == method && m.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(T), property.Type)
                    .Invoke(null, [query, lambda]);

                return (IQueryable<T>)result!;
            }
            return query;
        }

        public IQueryable<T> ApplyPaging<T>(IQueryable<T> query, BaseFilter? filter) where T : class, IEntity
        {
            if (filter?.Start is not null && filter?.Length is not null)
            {
                query = query.Skip(filter.Start.Value);
                query = query.Take(filter.Length.Value);
            }
            return query;
        }

    }
}
