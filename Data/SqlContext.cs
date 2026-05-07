using Bewegdeal.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data
{
    public class SqlContext(DbContextOptions<SqlContext> options) : DbContext(options)
    {
        public DbSet<UserEntity> Users => Set<UserEntity>();
        public DbSet<ReferenceEntity> References => Set<ReferenceEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── Users ────────────────────────────────────────────────────────────
            modelBuilder.Entity<UserEntity>(e =>
            {
                e.ToTable("Users");

                e.HasKey(u => u.Id);
                e.Property(u => u.Id).ValueGeneratedOnAdd();

                e.HasIndex(u => u.Id).IsUnique();
                e.HasIndex(u => u.Email).IsUnique();
                e.HasIndex(u => u.Role);
                e.HasIndex(u => u.Code);
                e.HasIndex(u => u.Status);

                e.Property(u => u.Password).IsRequired();
                e.Property(u => u.Salt).IsRequired();

                e.Property(u => u.Role).IsRequired().HasMaxLength(16);
                e.Property(u => u.Name).IsRequired().HasMaxLength(128);
                e.Property(u => u.Email).IsRequired().HasMaxLength(128);
                e.Property(u => u.Mobile).IsRequired().HasMaxLength(16);
                e.Property(u => u.Status).IsRequired().HasMaxLength(16);

                e.Property(u => u.Code).HasMaxLength(16);
                e.Property(u => u.Address).HasMaxLength(512);

                e.Property(u => u.Interests)
                    .HasMaxLength(128)
                    .HasConversion(
                        i => string.Join(',', i),
                        i => i.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    );
            });

            // ── References ───────────────────────────────────────────────────────
            modelBuilder.Entity<ReferenceEntity>(e =>
            {
                e.ToTable("References");

                e.HasKey(r => r.Id);
                e.HasIndex(r => r.Id).IsUnique();
                e.HasIndex(r => r.Type);
                e.Property(r => r.Id).ValueGeneratedNever().HasMaxLength(16);

                e.Property(r => r.Type).IsRequired().HasMaxLength(16);
                e.Property(r => r.Name).IsRequired().HasMaxLength(16);
            });
        }
    }
}
