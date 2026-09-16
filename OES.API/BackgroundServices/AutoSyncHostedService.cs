using Hangfire;
using OES.Core.Entities;
using OES.Helper.General;
using OES.Interface.Interfaces;
using OES.Interface.UnitOfWork;

namespace OES.API.BackgroundServices
{
    public class AutoSyncHostedService : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private const string CbtJobId = "auto-sync-daily-candidates";
        private const string VenueSyncJobId = "auto-sync-schedules-to-venues";
        private const string CandidateAnswersSyncJobId = "auto-sync-candidate-answers-to-eval";

        public AutoSyncHostedService(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            RegisterCbtCandidatesSyncJob();
            RegisterSchedulesAutoSyncJob();
            RegisterCandidateAnswersAutoSyncJob();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void RegisterCbtCandidatesSyncJob()
        {
            var (autoSyncEnabled, syncScheduleTime) = GetCBTSyncSettingsFromDb();

            if (!autoSyncEnabled)
            {
                var cbtSettings = _configuration.GetSection(MiscConstants.CBTApiSettingsSection).Get<CBTApiSettings>();

                if (cbtSettings == null || !cbtSettings.AutoSyncEnabled)
                    return;

                autoSyncEnabled = cbtSettings.AutoSyncEnabled;
                syncScheduleTime = cbtSettings.SyncScheduleTime;
            }

            var cronExpression = ConvertTimeToCron(syncScheduleTime);

            RecurringJob.AddOrUpdate<ICBTCandidatesSyncService>(
                CbtJobId,
                service => service.ExecuteSyncAutomaticallyAsync(null),
                cronExpression,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local
                }
            );
        }

        private void RegisterSchedulesAutoSyncJob()
        {
            var syncSettings = _configuration.GetSection(nameof(ExamServerSyncSettings)).Get<ExamServerSyncSettings>();

            if (syncSettings == null || !syncSettings.AutoSyncEnabled)
            {
                // Remove the job if it exists when auto-sync is disabled
                RecurringJob.RemoveIfExists(VenueSyncJobId);
                return;
            }

            RecurringJob.AddOrUpdate<IAutoSyncSchedulesService>(
                VenueSyncJobId,
                service => service.SyncRecurringJobsAsync(),
                Cron.Daily,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local
                }
            );

            RecurringJob.TriggerJob(VenueSyncJobId);
        }

        private void RegisterCandidateAnswersAutoSyncJob()
        {
            var syncSettings = _configuration
                .GetSection(nameof(CandidateAnswersSyncSettings))
                .Get<CandidateAnswersSyncSettings>();

            if (syncSettings?.AutoSyncEnabled != true)
            {
                RecurringJob.RemoveIfExists(CandidateAnswersSyncJobId);
                return;
            }

            var cronExpression = ConvertTimeToCron(syncSettings.SyncScheduleTime);

            RecurringJob.AddOrUpdate<IEvaluationSyncService>(
                CandidateAnswersSyncJobId,
                service => service.SyncCandidateAnswersToEvaluationAsync(),
                cronExpression,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local
                }
            );
        }

        private (bool AutoSyncEnabled, string SyncScheduleTime) GetCBTSyncSettingsFromDb()
        {
            using var scope = _serviceProvider.CreateScope();

            var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();

            if (unitOfWork == null)
                return (false, string.Empty);

            var setting = unitOfWork.Repository<CBTSyncSetting, long>()
                .GetAll()
                .FirstOrDefault(s => !s.IsDeleted);

            if (setting == null)
                return (false, string.Empty);

            return (setting.CBTAutoSyncEnabled, setting.CBTSyncScheduleTime);
        }

        private static string ConvertTimeToCron(string time)
        {
            var parts = time.Split(':');
            _ = int.TryParse(parts.ElementAtOrDefault(0), out int hour);
            _ = int.TryParse(parts.ElementAtOrDefault(1), out int minute);
            return $"{minute} {hour} * * *";
        }
    }
}
