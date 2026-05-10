using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;

namespace Bewegdeal.Data.Repositories
{
    public interface ITaskRepository : IRepository
    {
        Task<TaskEntity?>       Get    (TaskFilter filter);
        Task<List<TaskEntity>>  GetAll (TaskFilter filter);
        Task<TaskEntity>        Create (TaskEntity task);
        Task                    Update (TaskEntity task);
        Task                    Delete (TaskFilter filter);
    }
}
