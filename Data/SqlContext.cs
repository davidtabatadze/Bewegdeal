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
                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.Role).IsRequired().HasMaxLength(16);
                e.Property(u => u.Code).IsRequired().HasMaxLength(16);
                e.Property(u => u.Name).IsRequired().HasMaxLength(128);
                e.Property(u => u.Email).IsRequired().HasMaxLength(128);
                e.Property(u => u.Mobile).IsRequired().HasMaxLength(16);
                e.Property(u => u.Address).HasMaxLength(512);
                e.Property(u => u.Status).IsRequired().HasMaxLength(16);
                e.Property(u => u.Password).IsRequired();
                e.Property(u => u.Salt).IsRequired();
                e.Property(u => u.Interests)
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
            });

            // ── References ───────────────────────────────────────────────────────
            modelBuilder.Entity<ReferenceEntity>(e =>
            {
                e.ToTable("References");
                e.HasKey(r => r.Id);
                e.Property(r => r.Id).ValueGeneratedNever().HasMaxLength(16);
                e.Property(r => r.Type).IsRequired().HasMaxLength(16);
                e.Property(r => r.Name).IsRequired().HasMaxLength(16);
            });
        }
    }
}
