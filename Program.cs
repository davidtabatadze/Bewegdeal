using Bewegdeal.Data;
using Bewegdeal.Data.Base;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Tools;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Session
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromHours(8);
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            // Data / EF Core
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                                   ?? "Data Source=bewegdeal.db";

            builder.Services.AddDbContext<SqlContext>(options =>
                options.UseSqlite(connectionString));

            // Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IReferenceRepository, ReferenceRepository>();

            // Brevo
            BrevoTool.Configure(builder.Configuration);

            // Build
            var app = builder.Build();

            // Seed Data
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<SqlContext>();
                await context.Database.EnsureCreatedAsync();

                // References first — users depend on role values being present
                await ((IRepositorySeedable)scope.ServiceProvider.GetRequiredService<IReferenceRepository>()).Seed();
                await ((IRepositorySeedable)scope.ServiceProvider.GetRequiredService<IUserRepository>()).Seed();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseSession();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Landing}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
