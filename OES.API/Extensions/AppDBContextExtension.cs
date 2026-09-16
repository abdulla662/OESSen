using Microsoft.EntityFrameworkCore;
using OES.Infrastructure.Contexts;

namespace OES.API.Extensions
{
    public static class AppDbContextExtension
    {
        public static IServiceCollection AddAppContext(this IServiceCollection services, IConfiguration configuration)
        {
            string OESConStr = configuration.GetConnectionString("Default");

            services.AddDbContext<AppDbContext>(opt => opt.UseMySql(OESConStr, ServerVersion.AutoDetect(OESConStr)));

            services.AddDbContextFactory<AppDbContext>(opt => opt.UseMySql(OESConStr, ServerVersion.AutoDetect(OESConStr)), ServiceLifetime.Scoped);

            return services;
        }
    }
}