using OES.Helper.Dtos.Sync;

namespace OES.Blazor.Services.Implementation
{
    public class SyncDashboardState
    {
        public event Action<List<SyncJobStatusDto>> OnSyncStarted;

        public event Action<long, string> OnPreparationStarted;

        public event Action OnPreparationStopped;

        public event Action OnGlobalSyncStateChanged;

        // Global Sync States:
        public bool IsGlobalSyncActive { get; private set; }
        public DateTime? GlobalSyncUnlockTime { get; private set; }
        public long LockedOrganizationId { get; private set; }

        public void StartSync(List<SyncJobStatusDto> initialJobs)
        {
            OnSyncStarted?.Invoke(initialJobs);
        }

        public void StartPreparation(long scheduleId, string scheduleName)
        {
            OnPreparationStarted?.Invoke(scheduleId, scheduleName);
        }

        public void StopPreparation()
        {
            OnPreparationStopped?.Invoke();
        }

        public void SetGlobalSyncActive(bool isActive, DateTime? unlockTime = null, long orgId = 0)
        {
            IsGlobalSyncActive = isActive;
            GlobalSyncUnlockTime = unlockTime;
            LockedOrganizationId = orgId;
            OnGlobalSyncStateChanged?.Invoke();
        }
    }
}