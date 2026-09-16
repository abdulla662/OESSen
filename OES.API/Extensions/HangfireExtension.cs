using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MySql;
using System.Transactions;

namespace OES.API.Extensions
{
    public static class HangfireExtension
    {
        public static IServiceCollection AddHangfireOesServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("HangfireConnection");

            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            services.AddHangfire(config =>
            {
                config.UseStorage(new MySqlStorage(connectionString, new MySqlStorageOptions
                {
                    TablesPrefix = "Hangfire_",
                    TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    DashboardJobListLimit = 5000,
                    TransactionTimeout = TimeSpan.FromMinutes(1),
                    PrepareSchemaIfNecessary = true,
                }));

                config.UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings()
                      .UseDefaultTypeSerializer();
            });

            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 });

            services.AddHangfireServer(options => options.WorkerCount = 1);

            return services;
        }
    }

    public class AllowAllConnectionsAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // CAUTION: This allows anyone to access the dashboard. In production, implement proper authentication logic here
            return true;
        }
    }
}
