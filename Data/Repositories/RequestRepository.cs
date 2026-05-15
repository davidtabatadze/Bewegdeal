using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;

namespace Bewegdeal.Data.Repositories
{
    public class RequestRepository(SqlContext context) : IRequestRepository
    {
        public async Task<RequestEntity> Create(RequestEntity request)
        {
            context.Requests.Add(request);
            await context.SaveChangesAsync();
            return request;
        }
    }
}
