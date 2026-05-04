using AuthDotnetCoreJwt.Models.Domain;
using Microsoft.AspNetCore.Identity;

namespace AuthDotnetCoreJwt.Data.Seeders
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            await SeedRolesAsync(roleManager);
            await SeedAdminAsync(userManager);
        }

        // Seed roles
        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        // Seed admin user
        private static async Task SeedAdminAsync(UserManager<AppUser> userManager)
        {
            var adminEmail = "admin@gmail.com";

            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new AppUser
                {
                    FullName = "Admin",
                    Email = adminEmail,
                    UserName = adminEmail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(admin, "Admin@123");
            }

            if (!await userManager.IsInRoleAsync(admin, "Admin"))
                await userManager.AddToRoleAsync(admin, "Admin");

            if (!await userManager.IsInRoleAsync(admin, "User"))
                await userManager.AddToRoleAsync(admin, "User");
        }
    }
}
