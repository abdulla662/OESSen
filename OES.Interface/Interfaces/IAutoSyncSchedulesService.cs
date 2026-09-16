namespace OES.Interface.Interfaces
{
    public interface IAutoSyncSchedulesService
    {
        Task SyncRecurringJobsAsync();

        Task SyncSingleScheduleAsync(long scheduleId, long orgId, string orgSignature);

        void RegisterJobForSchedule(long scheduleId, long orgId, string orgSignature, TimeOnly? syncScheduleTime);

        void RemoveJobForSchedule(long scheduleId);
    }
}
