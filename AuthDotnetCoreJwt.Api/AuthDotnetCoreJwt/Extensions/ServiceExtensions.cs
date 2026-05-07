using AuthDotnetCoreJwt.Models.Dto.Auth;
using AuthDotnetCoreJwt.Repositories;
using AuthDotnetCoreJwt.Services;

namespace AuthDotnetCoreJwt.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddAppServices(
           this IServiceCollection services,
           IConfiguration config)
        {
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IEmailService, EmailService>();

            services.Configure<EmailSettings>(
                config.GetSection("EmailSettings"));

            return services;
        }
    }
}
