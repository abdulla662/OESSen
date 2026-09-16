using OES.Helper.Dtos.Sync;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces
{
    public interface ISyncToExamServer
    {
        Task<ApiResponse> BulkSyncToExamServerAsync(long scheduleId, List<long> scheduleSelectedVenues);

        Task<ApiResponse> RetryFailedJobsAsync(List<long> jobIds);

        Task<List<SyncJobStatusDto>> GetJobsListByBatchIdAsync(Guid batchId);

        Task<List<SyncJobStatusDto>> GetSyncHistoryAsync(DateTime fromDate);

        Task<ApiResponse> SyncByVenueAndDateAsync(string venueCode, DateTime examDate);

        Task<ApiResponse> CancelPendingJobAsync(long jobId);

        Task<byte[]?> GetPayloadBytesAsync(string fileName);

        Task<ApiResponse> ManualSyncAllVenuesAsync();

        Task<ApiResponse> GetCBTSyncStatusAsync();

        Task<ApiResponse> GetCentersSyncStatusAsync();

        Task<ApiResponse> ValidateScheduleForSyncAsync(long scheduleId);

        Task<ApiResponse> ToggleAutoSyncAsync(long scheduleId, bool isEnabled);

        Task<ApiResponse> GetAutoSyncStatusAsync(long scheduleId);

        Task<ApiResponse> StartManualSyncAsync(int numberOfDays = 2);

        Task<ApiResponse> IsSyncRunningAsync();
    }
}