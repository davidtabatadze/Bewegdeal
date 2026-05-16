using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;

namespace Bewegdeal.Data.Repositories
{
    public class RequestRepository(SqlContext context) : IRequestRepository
    {

        // ── Write ────────────────────────────────────────────────────────────────

        public async Task<RequestEntity> Create(RequestEntity request)
        {
            context.Requests.Add(request);
            await context.SaveChangesAsync();
            return request;
        }

        public async Task Update(RequestEntity request)
        {
            context.Requests.Update(request);
            await context.SaveChangesAsync();
        }

        // ── Read ─────────────────────────────────────────────────────────────────

        public async Task<RequestEntity?> Get(long id) =>
            await context.Requests.FindAsync(id);

        // ── Delete ───────────────────────────────────────────────────────────────
        // ***

    }
}
