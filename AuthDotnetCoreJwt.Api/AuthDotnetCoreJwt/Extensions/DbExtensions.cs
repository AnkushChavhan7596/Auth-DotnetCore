using AuthDotnetCoreJwt.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthDotnetCoreJwt.Extensions
{
    public static class DbExtensions
    {
        public static IServiceCollection AddAppDb(
            this IServiceCollection services,
            IConfiguration config)
        {
            services.AddDbContext<AuthDbContext>(options =>
            {
                options.UseSqlServer(config.GetConnectionString("DefaultConnnectionStrings"));
            });

            return services;
        }
    }
}
