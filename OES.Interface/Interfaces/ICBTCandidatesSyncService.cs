using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface ICBTCandidatesSyncService
    {
        Task<ApiResponse> SyncCBTCandidatesByVenueAndDateAsync(string venueCode, DateTime examDate);

        Task<ApiResponse> ExecuteSyncForDaysAsync();

        Task<ApiResponse> GetSyncJobDetailsAsync(long jobId);

        Task<ApiResponse> GetReceivedCandidatesByJobIdAsync(long jobId, PaginationSearchModel pagination);

        Task ExecuteSyncAutomaticallyAsync(Guid? callerUserId = null);

        Task<ApiResponse> GetCBTSyncStatusAsync();

        Task<ApiResponse> GetCentersSyncStatusAsync(DateTime? syncDate = null);

        Task<ApiResponse> StartManualSyncAsync(int numberOfDays = 2);

        Task<ApiResponse> IsSyncRunningAsync();

        Task<ApiResponse> NotifyOldVenueCesAsync(long registrationNumber, string venueCode, string modifiedByEmail);
    }
}