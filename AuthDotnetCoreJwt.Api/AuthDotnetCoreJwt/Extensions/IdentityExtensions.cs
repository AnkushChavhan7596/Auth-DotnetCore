using AuthDotnetCoreJwt.Data;
using AuthDotnetCoreJwt.Models.Domain;
using Microsoft.AspNetCore.Identity;

namespace AuthDotnetCoreJwt.Extensions
{
    public static class IdentityExtensions
    {
        public static IServiceCollection AddAppIdentity(
            this IServiceCollection services)
        {
            services.AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole>()
            .AddTokenProvider<DataProtectorTokenProvider<AppUser>>("testAuth")
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

            // Password configurations
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;
                options.Password.RequiredUniqueChars = 1;
            });

            return services;
        }
    }
}
