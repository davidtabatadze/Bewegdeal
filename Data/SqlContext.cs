using Bewegdeal.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bewegdeal.Data
{
    public class SqlContext(DbContextOptions<SqlContext> options, IConfiguration configuration) : DbContext(options)
    {
        private readonly string _prefix = configuration["Database:TablePrefix"] ?? "";

        #region DbSets
        public DbSet<UserEntity> Users => Set<UserEntity>();
        public DbSet<FileEntity> Files => Set<FileEntity>();
        public DbSet<SettingsEntity> Settings => Set<SettingsEntity>();
        public DbSet<RequestEntity> Requests => Set<RequestEntity>();
        public DbSet<RequestFileEntity> RequestFiles => Set<RequestFileEntity>();
        public DbSet<RequestAgreementEntity> RequestAgreements => Set<RequestAgreementEntity>();
        public DbSet<FraudWordEntity> FraudWords => Set<FraudWordEntity>();
        public DbSet<ChatEntity> Chats => Set<ChatEntity>();
        public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

        #endregion

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
                // MySQL does not support IF NOT EXISTS for indexes â€” handled via try-catch below instead.
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureUsers(modelBuilder);
            ConfigureFiles(modelBuilder);
            ConfigureSettings(modelBuilder);
            ConfigureRequests(modelBuilder);
            ConfigureRequestFiles(modelBuilder);
            ConfigureRequestAgreements(modelBuilder);
            ConfigureFraudWords(modelBuilder);
            ConfigureChats(modelBuilder);
            ConfigureChatMessages(modelBuilder);
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
                e.Property(u => u.ServiceTerms).HasMaxLength(256).IsRequired(false);
                e.Property(u => u.Avatar).HasMaxLength(256).IsRequired(false);
                e.Property(u => u.Theme).IsRequired().HasMaxLength(8).HasDefaultValue("light");
                e.Property(u => u.AcquaintedHIW).IsRequired().HasDefaultValue(false);
                e.Property(u => u.CreateDate).IsRequired();
                e.Property(u => u.TermsAndConditionsAcceptDate).IsRequired();
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

        private void ConfigureRequests(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RequestEntity>(e =>
            {
                e.ToTable(_prefix + "Requests");

                e.HasKey(r => r.Id);
                e.Property(r => r.Id).ValueGeneratedOnAdd();

                e.HasIndex(r => r.Number).IsUnique();
                e.HasIndex(r => r.Status);
                e.HasIndex(r => r.Service);
                e.HasIndex(r => r.ExecutorId);
                e.HasIndex(r => r.RequesterId);
                e.HasIndex(r => r.AgreementId);

                e.Property(r => r.Number).IsRequired().HasMaxLength(36);
                e.Property(r => r.CreateDate).IsRequired();
                e.Property(r => r.Status).IsRequired().HasMaxLength(16);
                e.Property(r => r.Service).IsRequired().HasMaxLength(16);
                e.Property(r => r.Title).IsRequired().HasMaxLength(64);
                e.Property(r => r.Description).IsRequired().HasMaxLength(2048);
                e.Property(r => r.PickupAddress).IsRequired().HasMaxLength(512);
                e.Property(r => r.DeliveryAddress).IsRequired().HasMaxLength(512);
                e.Property(r => r.RequesterId).IsRequired();
                e.Property(r => r.ExecutorId).IsRequired(false);
                e.Property(r => r.Cost).IsRequired().HasPrecision(18, 2);
                e.Property(r => r.Currency).IsRequired().HasMaxLength(4);
                e.Property(r => r.ASAP).IsRequired();
                e.Property(r => r.Date).IsRequired(false)
                    .HasConversion(
                        v => v.HasValue ? v.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                        v => v.HasValue ? DateOnly.FromDateTime(v.Value) : null
                    );
                e.Property(r => r.Time).IsRequired(false)
                    .HasConversion(
                        v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                        v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : null
                    );
                e.Property(r => r.AgreementId).IsRequired(false);
                e.Property(r => r.VehicleType).IsRequired(false).HasMaxLength(16);
                e.Property(r => r.VehicleCondition).IsRequired(false).HasMaxLength(16);
                e.Property(r => r.PresentElevator).IsRequired().HasDefaultValue(false);
                e.Property(r => r.PresentParking).IsRequired().HasDefaultValue(false);
            });
        }

        private void ConfigureRequestAgreements(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RequestAgreementEntity>(e =>
            {
                e.ToTable(_prefix + "RequestAgreements");

                e.HasKey(a => a.Id);
                e.Property(a => a.Id).ValueGeneratedOnAdd();

                e.HasIndex(a => a.Status);

                e.Property(a => a.CreateDate).IsRequired();
                e.Property(a => a.Cost).IsRequired().HasPrecision(18, 2);
                e.Property(a => a.Currency).IsRequired().HasMaxLength(4);
                e.Property(a => a.ServiceTermsFileId).IsRequired(false);
                e.Property(a => a.Status).IsRequired().HasMaxLength(16);
                e.Property(a => a.ReactionDate).IsRequired(false);
                e.Property(a => a.ReactionReason).IsRequired(false).HasMaxLength(1024);
                e.Property(a => a.Date).IsRequired(false)
                    .HasConversion(
                        v => v.HasValue ? v.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                        v => v.HasValue ? DateOnly.FromDateTime(v.Value) : null
                    );
                e.Property(a => a.Time).IsRequired(false)
                    .HasConversion(
                        v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                        v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : null
                    );
            });
        }

        private void ConfigureRequestFiles(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RequestFileEntity>(e =>
            {
                e.ToTable(_prefix + "RequestFiles");

                e.HasKey(f => f.Id);
                e.Property(f => f.Id).ValueGeneratedOnAdd();

                e.HasIndex(f => f.RequestId);
                e.HasIndex(f => f.IsMain);

                e.Property(f => f.Size).IsRequired();
                e.Property(f => f.IsMain).IsRequired();
                e.Property(f => f.RequestId).IsRequired();
                e.Property(f => f.Type).IsRequired().HasMaxLength(8);
                e.Property(f => f.File).IsRequired().HasMaxLength(256);
            });
        }

        private void ConfigureFraudWords(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FraudWordEntity>(e =>
            {
                e.ToTable(_prefix + "FraudWords");

                e.HasKey(w => w.Id);
                e.Property(w => w.Id).ValueGeneratedOnAdd();
                e.Property(w => w.Word).IsRequired().HasMaxLength(128);
            });
        }

        private void ConfigureChats(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatEntity>(e =>
            {
                e.ToTable(_prefix + "Chats");

                e.HasKey(c => c.Id);
                e.Property(c => c.Id).ValueGeneratedOnAdd();

                e.HasIndex(c => c.Key).IsUnique();
                e.HasIndex(c => c.RequestId);
                e.HasIndex(c => c.Status);

                e.Property(c => c.Key).IsRequired().HasMaxLength(32);
                e.Property(c => c.RequestId).IsRequired();
                e.Property(c => c.CustomerId).IsRequired();
                e.Property(c => c.CompanyId).IsRequired();
                e.Property(c => c.Status).IsRequired().HasMaxLength(16);
                e.Property(c => c.CreateDate).IsRequired();
            });
        }

        private void ConfigureChatMessages(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatMessageEntity>(e =>
            {
                e.ToTable(_prefix + "ChatMessages");

                e.HasKey(m => m.Id);
                e.Property(m => m.Id).ValueGeneratedOnAdd();

                e.HasIndex(m => m.ChatId);
                e.HasIndex(m => m.SenderId);

                e.Property(m => m.ChatId).IsRequired();
                e.Property(m => m.SenderId).IsRequired();
                e.Property(m => m.Content).IsRequired().HasMaxLength(2048);
                e.Property(m => m.SentDate).IsRequired();
                e.Property(m => m.IsRead).IsRequired().HasDefaultValue(false);
            });
        }

        private void ConfigureSettings(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SettingsEntity>(e =>
            {
                e.ToTable(_prefix + "Settings");

                e.HasKey(s => s.Id);
                e.Property(s => s.Id).ValueGeneratedNever();

                e.Property(s => s.TermsAndConditionsContent).IsRequired();
                e.Property(s => s.TermsAndConditionsContentDate).IsRequired();
                e.Property(s => s.RequestNegotiationMinutes).IsRequired();
                e.Property(s => s.RequestImageMaxCount).IsRequired();
                e.Property(s => s.RequestImageMaxSize).IsRequired();
                e.Property(s => s.RequestVideoMaxCount).IsRequired();
                e.Property(s => s.RequestVideoMaxSize).IsRequired();
            });
        }
    }
}
