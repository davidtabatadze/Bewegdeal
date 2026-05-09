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
        public DbSet<UserEntity>      Users      => Set<UserEntity>();
        public DbSet<ReferenceEntity> References => Set<ReferenceEntity>();
        public DbSet<TaskEntity>      Tasks      => Set<TaskEntity>();

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

            // Add any new columns to existing tables without a full migration system.
            // Each ALTER is silently ignored when the column already exists.
            await EnsureColumnsAsync(connection, isMySql);

            await connection.CloseAsync();
        }

        // Adds new columns to existing tables without a full migration system.
        // Checks existence via information_schema (MySQL) or pragma_table_info (SQLite)
        // before issuing ALTER TABLE, so it is always safe to run on every startup.
        private async Task EnsureColumnsAsync(System.Data.Common.DbConnection connection, bool isMySql)
        {
            string Q(string name) => isMySql ? $"`{name}`" : $"\"{name}\"";

            var columns = new (string table, string column, string mysqlType, string sqliteType)[]
            {
                (_prefix + "Tasks", "Currency",        "varchar(4)",    "TEXT"),
                (_prefix + "Tasks", "PickupAddress",   "varchar(512)",  "TEXT"),
                (_prefix + "Tasks", "DeliveryAddress", "varchar(512)",  "TEXT"),
                (_prefix + "Tasks", "Media",           "varchar(1024)", "TEXT"),
            };

            foreach (var (table, column, mysqlType, sqliteType) in columns)
            {
                // Check whether the column already exists
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = isMySql
                    ? $"SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}'"
                    : $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = '{column}'";

                var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (count > 0) { continue; } // column already present

                var type = isMySql ? mysqlType : sqliteType;
                var nullability = isMySql ? " NULL" : "";

                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = $"ALTER TABLE {Q(table)} ADD COLUMN {Q(column)} {type}{nullability}";
                await alterCmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Called once per application lifetime when EF Core builds the model.
        /// Each entity gets its own Configure* method for clarity.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureUsers(modelBuilder);
            ConfigureReferences(modelBuilder);
            ConfigureTasks(modelBuilder);
        }

        private void ConfigureUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserEntity>(e =>
            {
                e.ToTable(_prefix + "Users");

                e.HasKey(u => u.Id);
                e.Property(u => u.Id).ValueGeneratedOnAdd();

                // Indexes — Id and Email are unique
                e.HasIndex(u => u.Id).IsUnique();
                e.HasIndex(u => u.Email).IsUnique();
                e.HasIndex(u => u.Role);
                e.HasIndex(u => u.Status);
                e.HasIndex(u => u.Number);

                // Required fields
                e.Property(u => u.Password).IsRequired().HasMaxLength(64);
                e.Property(u => u.Salt).IsRequired();
                e.Property(u => u.Role).IsRequired().HasMaxLength(16);
                e.Property(u => u.Name).IsRequired().HasMaxLength(32);
                e.Property(u => u.Email).IsRequired().HasMaxLength(32);
                e.Property(u => u.Mobile).IsRequired().HasMaxLength(16);
                e.Property(u => u.Status).IsRequired().HasMaxLength(16);

                // Optional fields
                e.Property(u => u.Number).HasMaxLength(16);
                e.Property(u => u.Address).HasMaxLength(256);

                // Stored as a comma-separated string (e.g. "moving,pickup").
                // ValueComparer is required so EF Core can detect changes to array contents,
                // not just reference changes.
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

        private void ConfigureReferences(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReferenceEntity>(e =>
            {
                e.ToTable(_prefix + "References");

                // Id is a human-readable string key (e.g. "customer"), never auto-generated
                e.HasKey(r => r.Id);
                e.HasIndex(r => r.Id).IsUnique();
                e.HasIndex(r => r.Type);
                e.Property(r => r.Id).ValueGeneratedNever().HasMaxLength(16);

                e.Property(r => r.Type).IsRequired().HasMaxLength(16);
                e.Property(r => r.Name).IsRequired().HasMaxLength(16);
            });
        }

        private void ConfigureTasks(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskEntity>(e =>
            {
                e.ToTable(_prefix + "Tasks");

                e.HasKey(t => t.Id);
                e.Property(t => t.Id).ValueGeneratedOnAdd();

                e.HasIndex(t => t.Id).IsUnique();
                e.HasIndex(t => t.UserId);
                e.HasIndex(t => t.Status);
                e.HasIndex(t => t.Type);

                e.Property(t => t.UserId).IsRequired();
                e.Property(t => t.Type).IsRequired().HasMaxLength(16);
                e.Property(t => t.Name).IsRequired().HasMaxLength(128);
                e.Property(t => t.Description).HasMaxLength(512);
                e.Property(t => t.Image).HasMaxLength(256);
                e.Property(t => t.Media).HasMaxLength(1024);
                e.Property(t => t.Cost).HasPrecision(10, 2);
                e.Property(t => t.Currency).HasMaxLength(4);
                e.Property(t => t.PickupAddress).HasMaxLength(512);
                e.Property(t => t.DeliveryAddress).HasMaxLength(512);
                e.Property(t => t.Status).IsRequired().HasMaxLength(16);
                e.Property(t => t.CreatedAt).IsRequired();
            });
        }
    }
}
