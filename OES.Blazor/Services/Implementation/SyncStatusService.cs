using Blazored.LocalStorage;
using OES.Blazor.Services.Interfaces;
using OES.Helper.Dtos.Sync;
using OES.Helper.General;
using SharedHelper.Enums;

namespace OES.Blazor.Services.Implementation
{
    public class SyncStatusService : ISyncStatusService
    {
        private readonly List<SyncJobStatusDto> _allJobs = new();

        private readonly IServiceProvider _serviceProvider;

        public event Action StatusChanged;

        public bool HasActiveJobs => _allJobs.Any(j =>
            j.Status == SyncJobStatus.Pending ||
            j.Status == SyncJobStatus.InProgress
        );

        public bool HasAnyJobs => _allJobs.Any();

        public int ActiveJobCount => _allJobs.Count(j =>
            j.Status == SyncJobStatus.Pending ||
            j.Status == SyncJobStatus.InProgress
        );

        public int FailedJobCount => _allJobs.Count(j => j.Status == SyncJobStatus.Failed);

        public SyncStatusService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task HydrateStateFromStorageAsync()
        {
            using var scope = _serviceProvider.CreateScope();

            var localStorage = scope.ServiceProvider.GetRequiredService<ILocalStorageService>();

            var syncToServerService = scope.ServiceProvider.GetRequiredService<ISyncToExamServer>();

            var lastBatchId = await localStorage.GetItemAsync<Guid?>(MiscConstants.LAST_SYNC_BATCH_ID_KEY);

            if (lastBatchId.HasValue && lastBatchId.Value != Guid.Empty)
            {
                var jobs = await syncToServerService.GetJobsListByBatchIdAsync(lastBatchId.Value);

                if (jobs != null)
                {
                    _allJobs.Clear();
                    _allJobs.AddRange(jobs);
                    StatusChanged?.Invoke();
                }
            }
        }

        public List<SyncJobStatusDto> GetAllJobs() => _allJobs.ToList();

        public void AddJobs(List<SyncJobStatusDto> jobs)
        {
            _allJobs.Clear();

            _allJobs.AddRange(jobs);

            var batchId = jobs.FirstOrDefault()?.BatchId;

            if (batchId.HasValue && batchId.Value != Guid.Empty)
            {
                _ = Task.Run(async () =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var localStorage = scope.ServiceProvider.GetRequiredService<ILocalStorageService>();
                    await localStorage.SetItemAsync(MiscConstants.LAST_SYNC_BATCH_ID_KEY, batchId.Value);
                });
            }

            StatusChanged?.Invoke();
        }

        public void UpdateJob(long jobId, SyncJobStatus status, string errorMessage, DateTime? completedAt)
        {
            var job = _allJobs.FirstOrDefault(j => j.Id == jobId);

            if (job != null)
            {
                job.Status = status;
                job.ErrorMessage = errorMessage;
                job.CompletedAt = completedAt;
                StatusChanged?.Invoke();
            }
        }
    }
}