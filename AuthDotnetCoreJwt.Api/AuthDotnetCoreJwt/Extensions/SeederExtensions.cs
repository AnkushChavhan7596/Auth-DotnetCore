using AuthDotnetCoreJwt.Data.Seeders;
using AuthDotnetCoreJwt.Models.Domain;
using Microsoft.AspNetCore.Identity;

namespace AuthDotnetCoreJwt.Extensions
{
    public static class SeederExtensions
    {
        public static async Task SeedDatabaseAsync(
            this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var services = scope.ServiceProvider;

            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await IdentitySeeder.SeedAsync(userManager, roleManager);
        }
    }
}
