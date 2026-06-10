using Bewegdeal.Data;
using Bewegdeal.Data.Base;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Hubs;
using Bewegdeal.Services;
using Bewegdeal.Tools;
using Microsoft.AspNetCore.Authentication.Cookies;
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

            // ── SignalR ───────────────────────────────────────────────────────────
            builder.Services.AddSignalR();

            // ── Authentication ────────────────────────────────────────────────────
            // Cookie auth is the primary scheme for in-scope controllers ([Authorize]).
            // AccessDeniedPath mirrors the old RequireAdminAttribute redirect target.
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o =>
                {
                    o.LoginPath = "/Account/Login";
                    o.AccessDeniedPath = "/Home/Index";
                    o.ExpireTimeSpan = TimeSpan.FromHours(8);
                    o.SlidingExpiration = true;
                });

            // ── Cache ─────────────────────────────────────────────────────────────
            // Used by StatusRefreshMiddleware to throttle per-user DB status checks.
            builder.Services.AddMemoryCache();

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
            builder.Services.AddScoped<IFileRepository, FileRepository>();
            builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
            builder.Services.AddScoped<IRequestRepository, RequestRepository>();
            builder.Services.AddScoped<IRequestFileRepository, RequestFileRepository>();
            builder.Services.AddScoped<IFraudWordRepository, FraudWordRepository>();
            builder.Services.AddScoped<IChatRepository, ChatRepository>();

            // ── Storage ───────────────────────────────────────────────────────────
            // Files are stored on the local file system.
            // Base path is read from Storage:Local:Path in appsettings.json.
            builder.Services.AddSingleton<IFileStorageTool, FileStorageTool>();

            // ── Services ──────────────────────────────────────────────────────────
            // Scoped because FileService wraps IFileRepository (also scoped).
            builder.Services.AddScoped<BrevoService>();
            builder.Services.AddScoped<FileService2>();
            builder.Services.AddScoped<SettingService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<AccountService>();
            builder.Services.AddScoped<ChatService>();
            builder.Services.AddScoped<ChatHubService>();
            builder.Services.AddScoped<RequestService>();
            builder.Services.AddScoped<RequestChatService>();

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

                await ((IRepositorySeedable)scope.ServiceProvider.GetRequiredService<IUserRepository>()).Seed();
                await ((IRepositorySeedable)scope.ServiceProvider.GetRequiredService<ISettingsRepository>()).Seed();
            }

            // ── Middleware pipeline ───────────────────────────────────────────────
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseMiddleware<UserRefreshTool>();
            app.UseAuthorization();

            // ── Routes ────────────────────────────────────────────────────────────
            // Default route lands on the public landing page, not the admin dashboard.
            app.MapStaticAssets();
            app.MapHub<ChatHub>("/hubs/chat");
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Landing}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
