using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class SyncCandidatesController : OESBaseController, ICBTCandidatesSyncService
    {
        private readonly ICBTCandidatesSyncService _cbtCandidatesSyncService;

        public SyncCandidatesController(ICBTCandidatesSyncService cbtCandidatesSyncService)
        {
            _cbtCandidatesSyncService = cbtCandidatesSyncService;
        }

        [HttpGet("CBTSyncStatus")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetCBTSyncStatusAsync()
        {
            return await _cbtCandidatesSyncService.GetCBTSyncStatusAsync();
        }

        [HttpGet("CentersSyncStatus")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetCentersSyncStatusAsync(DateTime? syncDate = null)
        {
            return await _cbtCandidatesSyncService.GetCentersSyncStatusAsync(syncDate);
        }

        [HttpPost("ReceivedCandidates/{jobId}")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetReceivedCandidatesAsync(long jobId, [FromBody] PaginationSearchModel pagination)
        {
            return await _cbtCandidatesSyncService.GetReceivedCandidatesByJobIdAsync(jobId, pagination);
        }

        [HttpPost("ManualSyncByVenueDate")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> SyncCBTCandidatesByVenueAndDateAsync(string venueCode, DateTime examDate)
        {
            return await _cbtCandidatesSyncService.SyncCBTCandidatesByVenueAndDateAsync(venueCode, examDate);
        }

        [HttpPost("SyncTodayTomorrow")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> ExecuteSyncForDaysAsync()
        {
            return await _cbtCandidatesSyncService.ExecuteSyncForDaysAsync();
        }

        [HttpPost("SyncToday")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetSyncJobDetailsAsync(long jobId)
        {
            return await _cbtCandidatesSyncService.ExecuteSyncForDaysAsync();
        }

        [HttpGet("JobStatus/{jobId}")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetReceivedCandidatesByJobIdAsync(long jobId, PaginationSearchModel pagination)
        {
            return await _cbtCandidatesSyncService.GetSyncJobDetailsAsync(jobId);
        }

        // TODO: Don't delete this method because we may need it in the future
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("ExecuteSyncAutomaticallyAsync")]
        //[OESFilter(Authorize = true)]
        public Task ExecuteSyncAutomaticallyAsync(Guid? callerUserId = null)
        {
            return _cbtCandidatesSyncService.ExecuteSyncAutomaticallyAsync(callerUserId);
        }

        [HttpPost("StartManualSync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> StartManualSyncAsync(int numberOfDays = 2)
        {
            return await _cbtCandidatesSyncService.StartManualSyncAsync(numberOfDays);
        }

        [HttpGet("IsSyncRunning")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> IsSyncRunningAsync()
        {
            return await _cbtCandidatesSyncService.IsSyncRunningAsync();
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public Task<ApiResponse> NotifyOldVenueCesAsync(long registrationNumber, string venueCode, string modifiedByEmail)
        {
            return _cbtCandidatesSyncService.NotifyOldVenueCesAsync(registrationNumber, venueCode, modifiedByEmail);
        }
    }
}
