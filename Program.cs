using Bewegdeal.Data;
using Bewegdeal.Data.Base;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Middleware;
using Bewegdeal.Tools;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── MVC ──────────────────────────────────────────────────────────────
            builder.Services.AddControllersWithViews();

            // ── Session ───────────────────────────────────────────────────────────
            // HttpOnly + SameAsRequest keeps the cookie secure without forcing HTTPS in dev.
            // 8-hour idle timeout matches a typical working day.
            builder.Services.AddMemoryCache();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromHours(8);
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            // ── Database ──────────────────────────────────────────────────────────
            // Provider and connection strings are read from Database section in appsettings.json.
            // Valid providers: "sqlite" (local dev) | "mysql" (production).
            var dbConfiguration = builder.Configuration.GetSection("Database");
            var dbProvider = dbConfiguration["Provider"] ?? "";

            if (!new List<string> { "sqlite", "mysql" }.Contains(dbProvider))
            {
                throw new InvalidOperationException(
                    $"Unsupported database provider '{dbProvider}'. Set Database:Provider to 'sqlite' or 'mysql' in appsettings.json."
                );
            }

            builder.Services.AddDbContext<SqlContext>(options =>
            {
                if (dbProvider == "sqlite")
                {
                    options.UseSqlite(dbConfiguration["Sqlite"] ?? "Data Source=bewegdeal.db");
                }
                if (dbProvider == "mysql")
                {
                    options.UseMySQL(dbConfiguration["MySql"] ?? "Server=localhost;Database=bewegdeal;User=root;Password=;");
                }
            });

            // ── Repositories ──────────────────────────────────────────────────────
            // Scoped per request — each request gets its own DbContext and repository instance.
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IReferenceRepository, ReferenceRepository>();
            builder.Services.AddScoped<IFileRepository, FileRepository>();

            // ── Storage ───────────────────────────────────────────────────────────
            // Files are stored on the local file system.
            // Base path is read from Storage:Local:Path in appsettings.json.
            builder.Services.AddSingleton<IFileStorageTool, FileStorageTool>();

            // ── Email ─────────────────────────────────────────────────────────────
            // Reads Brevo:ApiKey, Brevo:FromEmail, Brevo:FromName from appsettings.json.
            BrevoTool.Configure(builder.Configuration);

            // ── Build ─────────────────────────────────────────────────────────────
            var app = builder.Build();

            // ── Startup: schema + seed ────────────────────────────────────────────
            // EnsureTablesAsync creates any missing tables using IF NOT EXISTS — safe on every run.
            // References must be seeded before Users (users reference role values).
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<SqlContext>();
                await context.EnsureTablesAsync();

                await ((IRepositorySeedable)scope.ServiceProvider.GetRequiredService<IReferenceRepository>()).Seed();
                await ((IRepositorySeedable)scope.ServiceProvider.GetRequiredService<IUserRepository>()).Seed();
            }

            // ── Middleware pipeline ───────────────────────────────────────────────
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseSession();
            app.UseMiddleware<RememberMeMiddleware>();
            app.UseAuthorization();

            // ── Routes ────────────────────────────────────────────────────────────
            // Default route lands on the public landing page, not the admin dashboard.
            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Landing}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
