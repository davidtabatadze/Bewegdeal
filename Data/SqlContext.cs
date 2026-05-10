using Bewegdeal.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bewegdeal.Data
{
    public class SqlContext(DbContextOptions<SqlContext> options, IConfiguration configuration) : DbContext(options)
    {
        // Table prefix read from Database:TablePrefix in appsettings.json (e.g. "dev_")
        private readonly string _prefix = configuration["Database:TablePrefix"] ?? "";

        #region DbSets
        public DbSet<UserEntity> Users => Set<UserEntity>();
        public DbSet<FileEntity> Files => Set<FileEntity>();
        public DbSet<ReferenceEntity> References => Set<ReferenceEntity>();

        #endregion

        /// <summary>
        /// Ensures the database and all tables exist on startup.
        /// Uses IF NOT EXISTS for every DDL statement so the method is safe
        /// to call on every run — existing tables and indexes are never touched.
        /// </summary>
        public async Task EnsureTablesAsync()
        {
            var database = Database.GetService<IRelationalDatabaseCreator>();

            // Create the database itself if it does not exist yet
            if (!await database.ExistsAsync())
            {
                await database.CreateAsync();
            }

            var connection = Database.GetDbConnection();
            await connection.OpenAsync();

            // Generate the full DDL script from the current EF Core model and
            // patch every CREATE statement to be idempotent before executing
            var statements = Database.GenerateCreateScript()
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim())
                .Where(i => i.Length > 0);

            var isMySql = Database.ProviderName?.Contains("MySql") ?? false;

            foreach (var statement in statements)
            {
                var isIndex = statement.StartsWith("CREATE INDEX") ||
                              statement.StartsWith("CREATE UNIQUE INDEX");

                // UNIQUE must be replaced before INDEX to avoid partial match.
                // MySQL does not support IF NOT EXISTS for indexes — handled via try-catch below instead.
                var sql = statement.Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ");

                if (!isMySql)
                {
                    sql = sql.Replace("CREATE INDEX ", "CREATE INDEX IF NOT EXISTS ")
                             .Replace("CREATE UNIQUE INDEX ", "CREATE UNIQUE INDEX IF NOT EXISTS ");
                }

                using var command = connection.CreateCommand();
                command.CommandText = sql;

                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch when (isMySql && isIndex)
                {
                    // MySQL does not support IF NOT EXISTS for indexes;
                    // ignore "index already exists" errors
                }
            }

            await connection.CloseAsync();
        }

        /// <summary>
        /// Called once per application lifetime when EF Core builds the model.
        /// Each entity gets its own Configure* method for clarity.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureUsers(modelBuilder);
            ConfigureFiles(modelBuilder);
            ConfigureReferences(modelBuilder);
        }

        private void ConfigureUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserEntity>(e =>
            {
                e.ToTable(_prefix + "Users");

                e.HasKey(u => u.Id);
                e.Property(u => u.Id).ValueGeneratedOnAdd();

                e.HasIndex(u => u.Id).IsUnique();
                e.HasIndex(u => u.Email).IsUnique();
                e.HasIndex(u => u.Role);
                e.HasIndex(u => u.Status);
                e.HasIndex(u => u.Number);

                e.Property(u => u.Password).IsRequired().HasMaxLength(64);
                e.Property(u => u.Salt).IsRequired();
                e.Property(u => u.Role).IsRequired().HasMaxLength(16);
                e.Property(u => u.Name).IsRequired().HasMaxLength(32);
                e.Property(u => u.Email).IsRequired().HasMaxLength(32);
                e.Property(u => u.Mobile).IsRequired().HasMaxLength(16);
                e.Property(u => u.Status).IsRequired().HasMaxLength(16);

                e.Property(u => u.Number).HasMaxLength(16);
                e.Property(u => u.Address).HasMaxLength(256);

                e.Property(u => u.Interests)
                    .HasMaxLength(128)
                    .HasConversion(
                        i => string.Join(',', i),
                        i => i.Split(',', StringSplitOptions.RemoveEmptyEntries),
                        new ValueComparer<string[]>(
                            (a, b) => a!.SequenceEqual(b!),
                            i => i.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                            i => i.ToArray()
                        )
                    );
            });
        }

        private void ConfigureFiles(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileEntity>(e =>
            {
                e.ToTable(_prefix + "Files");

                e.HasKey(f => f.Id);
                e.Property(f => f.Id).ValueGeneratedOnAdd();

                e.HasIndex(f => f.Key).IsUnique();

                e.Property(f => f.Size).IsRequired();
                e.Property(f => f.Key).IsRequired().HasMaxLength(64);
                e.Property(f => f.MimeType).IsRequired().HasMaxLength(16);
                e.Property(f => f.FileName).IsRequired().HasMaxLength(256);
            });
        }

        private void ConfigureReferences(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReferenceEntity>(e =>
            {
                e.ToTable(_prefix + "References");

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
