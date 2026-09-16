using OES.Helper.Dtos.Sync;
using SharedHelper.Enums;

namespace OES.Blazor.Services.Interfaces
{
    public interface ISyncStatusService
    {
        event Action StatusChanged;

        bool HasActiveJobs { get; }

        bool HasAnyJobs { get; }

        int ActiveJobCount { get; }

        int FailedJobCount { get; }

        Task HydrateStateFromStorageAsync();

        List<SyncJobStatusDto> GetAllJobs();

        void AddJobs(List<SyncJobStatusDto> jobs);

        void UpdateJob(long jobId, SyncJobStatus status, string errorMessage, DateTime? completedAt);
    }
}