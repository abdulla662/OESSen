using Hangfire;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.Sync;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Interface.Interfaces;
using OES.Services.ParallelService;

namespace OES.Services.Services;

public class AutoSyncSchedulesService : IAutoSyncSchedulesService
{
    private readonly ParallelQueryService _parallelQueryService;
    private readonly IExamServerService _examServerService;
    private readonly ILogger<AutoSyncSchedulesService> _logger;
    private readonly IConfiguration _configuration;

    public AutoSyncSchedulesService(
        ParallelQueryService parallelQueryService,
        IExamServerService examServerService,
        ILogger<AutoSyncSchedulesService> logger,
        IConfiguration configuration
    )
    {
        _parallelQueryService = parallelQueryService;
        _examServerService = examServerService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SyncRecurringJobsAsync()
    {
        var (globalHour, globalMin) = GetGlobalSyncTime();

        var allSchedules = await _parallelQueryService.ExecuteReadAsync<ScheduleMetadata, long, List<(long Id, long OrgId, string OrgSignature, TimeOnly? SyncScheduleTime)>>(
            repository => repository
                .Query()
                .Where(s => s.IsAutoSyncEnabled && s.PublishingStatus == PublishingStatus.Published && s.EndDate >= DateOnly.FromDateTime(DateTime.Today))
                .Select(s => new ValueTuple<long, long, string, TimeOnly?>(
                    s.Id,
                    s.OrganizationId,
                    s.OrganizationSignature,
                    s.SyncScheduleTime
                ))
                .ToListAsync()
        );

        var activeJobIds = new HashSet<string>();

        foreach (var schedule in allSchedules)
        {
            var hour = schedule.SyncScheduleTime?.Hour ?? globalHour;
            var minute = schedule.SyncScheduleTime?.Minute ?? globalMin;
            var cron = $"{minute} {hour} * * *";
            var jobId = $"auto-sync-schedule-{schedule.Id}";

            RecurringJob.AddOrUpdate<IAutoSyncSchedulesService>(
                jobId,
                service => service.SyncSingleScheduleAsync(schedule.Id, schedule.OrgId, schedule.OrgSignature),
                cron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }
            );

            activeJobIds.Add(jobId);
        }

        // Remove jobs for schedules that are no longer eligible
        var existingJobs = JobStorage.Current
            .GetConnection()
            .GetRecurringJobs()
            .Where(j => j.Id.StartsWith("auto-sync-schedule-"))
            .ToList();

        foreach (var job in existingJobs.Where(j => !activeJobIds.Contains(j.Id)))
        {
            RecurringJob.RemoveIfExists(job.Id);

            _logger.LogInformation("Removed stale auto-sync job {JobId}", job.Id);
        }
    }

    public async Task SyncSingleScheduleAsync(long scheduleId, long orgId, string orgSignature)
    {
        _logger.LogInformation("Auto-syncing schedule {ScheduleId}", scheduleId);

        // Guard: Remove the job and stop if the schedule has expired
        var endDate = await _parallelQueryService.ExecuteReadAsync<ScheduleMetadata, long, DateOnly?>(
            repository => repository
                .Query()
                .Where(s => s.Id == scheduleId)
                .Select(s => (DateOnly?)s.EndDate)
                .FirstOrDefaultAsync()
        );

        if (endDate == null || endDate.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            _logger.LogInformation(
                "Schedule {ScheduleId} has expired (EndDate: {EndDate}). Removing auto-sync job.",
                scheduleId,
                endDate
            );

            RemoveJobForSchedule(scheduleId);
            return;
        }

        try
        {
            var audit = AuditContextDto.FromSchedule(orgId, orgSignature);

            await _examServerService.BulkSync(scheduleId, isAutoSync: true, audit: audit);

            _logger.LogInformation("Auto-sync completed for schedule {ScheduleId}", scheduleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-sync failed for schedule {ScheduleId}", scheduleId);

            throw; // Let Hangfire mark it as failed & retry
        }
    }

    public void RegisterJobForSchedule(long scheduleId, long orgId, string orgSignature, TimeOnly? syncScheduleTime)
    {
        var (globalHour, globalMin) = GetGlobalSyncTime();

        var hour = syncScheduleTime?.Hour ?? globalHour;
        var minute = syncScheduleTime?.Minute ?? globalMin;
        var cron = $"{minute} {hour} * * *";
        var jobId = $"auto-sync-schedule-{scheduleId}";

        RecurringJob.AddOrUpdate<IAutoSyncSchedulesService>(
            jobId,
            service => service.SyncSingleScheduleAsync(scheduleId, orgId, orgSignature),
            cron,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }
        );

        _logger.LogInformation("Registered/updated auto-sync job {JobId} with cron {Cron}", jobId, cron);
    }

    public void RemoveJobForSchedule(long scheduleId)
    {
        var jobId = $"auto-sync-schedule-{scheduleId}";
        RecurringJob.RemoveIfExists(jobId);
        _logger.LogInformation("Removed auto-sync job {JobId}", jobId);
    }

    #region Helpers
    private (int Hour, int Minute) GetGlobalSyncTime()
    {
        var settings = _configuration.GetSection(nameof(ExamServerSyncSettings)).Get<ExamServerSyncSettings>();
        var globalTime = settings?.SyncScheduleTime ?? "03:00";
        var globalParts = globalTime.Split(':');
        var hour = globalParts.Length > 0 && int.TryParse(globalParts[0], out var gh) ? gh : 3;
        var minute = globalParts.Length > 1 && int.TryParse(globalParts[1], out var gm) ? gm : 0;
        return (hour, minute);
    }
    #endregion
}
