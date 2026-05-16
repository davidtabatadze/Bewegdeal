using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IReferenceRepository : IRepository
    {
        Task<ReferenceEntity> Create(ReferenceEntity reference);
        Task Update(ReferenceEntity reference);
        Task<ReferenceEntity?> Get(string id);
    }
}
