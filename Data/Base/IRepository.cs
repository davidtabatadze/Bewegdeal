namespace Bewegdeal.Data.Base
{
    public interface IRepository
    {
        Task<T> Create<T>(T entity) where T : class, IEntity;
        Task Create<T>(IEnumerable<T> entities) where T : class, IEntity;
        Task Update<T>(T entity) where T : class, IEntity;
        Task Delete<T>(long id) where T : class, IEntity;
        Task Delete<T>(List<long> ids) where T : class, IEntity;
        Task<T?> Get<T>(long id, string[]? properties = null) where T : class, IEntity;
        Task<List<T>> Load<T>(IEnumerable<long> ids, string[]? properties = null) where T : class, IEntity;
    }
}
