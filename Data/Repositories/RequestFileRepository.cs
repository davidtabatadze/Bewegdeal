using Bewegdeal.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class RequestFileRepository(SqlContext context) : IRequestFileRepository
    {
        private readonly SqlContext _context = context;

        // ── Write ─────────────────────────────────────────────────────────────────

        public async Task<RequestFileEntity> Create(RequestFileEntity file)
        {
            _context.RequestFiles.Add(file);
            await _context.SaveChangesAsync();
            return file;
        }

        // ── Read ─────────────────────────────────────────────────────────────────

        public async Task<List<RequestFileEntity>> Load(long requestId)
        {
            return await _context.RequestFiles
                                 .Where(f => f.RequestId == requestId)
                                 .ToListAsync();
        }

        // ── Delete ────────────────────────────────────────────────────────────────

        public async Task Delete(long requestId)
        {
            await _context.RequestFiles
                          .Where(f => f.RequestId == requestId)
                          .ExecuteDeleteAsync();
        }
    }
}
