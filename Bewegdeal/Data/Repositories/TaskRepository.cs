using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class TaskRepository(SqlContext context) : ITaskRepository, IRepositorySeedable
    {
        private readonly SqlContext _context = context;

        public async Task Seed()
        {
            if (await _context.Tasks.AnyAsync()) { return; }

            var tasks = new[]
            {
                new TaskEntity
                {
                    UserId      = 1,
                    Type        = ServiceEnum.Moving,
                    Name        = "Move from Vienna to Graz",
                    Description = "3-room apartment, 3rd floor, no elevator",
                    Cost        = 850.00m,
                    Status      = TaskStatusEnum.Active,
                    Views       = 12,
                    CreatedAt   = new DateTime(2026, 4, 15)
                },
                new TaskEntity
                {
                    UserId      = 1,
                    Type        = ServiceEnum.Removal,
                    Name        = "Old furniture disposal",
                    Description = "Sofa, wardrobe and mattress removal",
                    Cost        = 250.00m,
                    Status      = TaskStatusEnum.Pending,
                    Views       = 5,
                    CreatedAt   = new DateTime(2026, 4, 22)
                },
                new TaskEntity
                {
                    UserId      = 1,
                    Type        = ServiceEnum.Pickup,
                    Name        = "IKEA order pickup and delivery",
                    Description = "Large order from IKEA Wien Nord",
                    Cost        = 180.00m,
                    Status      = TaskStatusEnum.Completed,
                    Views       = 8,
                    CreatedAt   = new DateTime(2026, 3, 10)
                },
                new TaskEntity
                {
                    UserId      = 1,
                    Type        = ServiceEnum.Transport,
                    Name        = "Car transport Vienna to Salzburg",
                    Description = "Standard sedan, non-running vehicle",
                    Cost        = 420.00m,
                    Status      = TaskStatusEnum.Active,
                    Views       = 19,
                    CreatedAt   = new DateTime(2026, 5, 1)
                },
                new TaskEntity
                {
                    UserId      = 1,
                    Type        = ServiceEnum.Moving,
                    Name        = "Office relocation in downtown Vienna",
                    Description = "10 desks, cabinets and IT equipment",
                    Cost        = 1200.00m,
                    Status      = TaskStatusEnum.Pending,
                    Views       = 3,
                    CreatedAt   = new DateTime(2026, 5, 7)
                },
                new TaskEntity
                {
                    UserId      = 1,
                    Type        = ServiceEnum.Removal,
                    Name        = "Construction debris cleanup",
                    Description = "Renovation waste from bathroom remodel",
                    Cost        = 320.00m,
                    Status      = TaskStatusEnum.Cancelled,
                    Views       = 7,
                    CreatedAt   = new DateTime(2026, 3, 28)
                }
            };

            foreach (var task in tasks)
            {
                await Create(task);
            }
        }

        // ── Read ─────────────────────────────────────────────────────────────────

        public async Task<TaskEntity?> Get(TaskFilter filter)
        {
            var query = _context.Tasks.AsQueryable();

            if (filter.Id.HasValue)     { query = query.Where(t => t.Id     == filter.Id.Value);     }
            if (filter.UserId.HasValue) { query = query.Where(t => t.UserId == filter.UserId.Value); }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<TaskEntity>> GetAll(TaskFilter filter)
        {
            var query = _context.Tasks.AsQueryable();

            if (filter.Id.HasValue)     { query = query.Where(t => t.Id     == filter.Id.Value);     }
            if (filter.UserId.HasValue) { query = query.Where(t => t.UserId == filter.UserId.Value); }

            return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }

        // ── Write ────────────────────────────────────────────────────────────────

        public async Task<TaskEntity> Create(TaskEntity task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task Update(TaskEntity task)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
        }
    }
}
