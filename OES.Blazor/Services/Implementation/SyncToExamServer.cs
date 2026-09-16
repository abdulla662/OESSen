using OES.Blazor.Services.Interfaces;
using OES.Helper.Dtos.CBTCandidates;
using OES.Helper.Dtos.Schedule;
using OES.Helper.Dtos.Sync;
using OES.Helper.General;
using SharedHelper.General;

namespace OES.Blazor.Services.Implementation
{
    public class SyncToExamServer : ISyncToExamServer
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public SyncToExamServer(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<ApiResponse> BulkSyncToExamServerAsync(long scheduleId, List<long> scheduleSelectedVenues)
        {
            return await _httpClientHelper.PostAsync(scheduleSelectedVenues, $"api/ExamServer/BulkSync?{nameof(scheduleId)}={scheduleId}");
        }

        public async Task<ApiResponse> ValidateScheduleForSyncAsync(long scheduleId)
        {
            return await _httpClientHelper.GetAsync<object>($"api/ExamServer/ValidateScheduleForSync?{nameof(scheduleId)}={scheduleId}");
        }

        public async Task<ApiResponse> StartManualSyncAsync(int numberOfDays = 2)
        {
            return await _httpClientHelper.PostAsync(null, $"api/SyncCandidates/StartManualSync?{nameof(numberOfDays)}={numberOfDays}");
        }

        public async Task<ApiResponse> IsSyncRunningAsync()
        {
            return await _httpClientHelper.GetAsync<bool>("api/SyncCandidates/IsSyncRunning");
        }

        #region RabbitMQJobManagment

        public async Task<ApiResponse> RetryFailedJobsAsync(List<long> jobIds)
        {
            var request = new RetryFailedJobsRequest(jobIds);

            return await _httpClientHelper.PostAsync(request, "api/JobManagement/RetryFailedJobs");
        }

        public async Task<List<SyncJobStatusDto>> GetJobsListByBatchIdAsync(Guid batchId)
        {
            var response = await _httpClientHelper.GetAsync<List<SyncJobStatusDto>>($"api/JobManagement/GetJobsByBatchId?{nameof(batchId)}={batchId}");

            return (List<SyncJobStatusDto>)response.Data;
        }

        public async Task<List<SyncJobStatusDto>> GetSyncHistoryAsync(DateTime fromDate)
        {
            var response = await _httpClientHelper.GetAsync<List<SyncJobStatusDto>>($"api/JobManagement/GetSyncHistory?{nameof(fromDate)}={fromDate:O}");

            return (List<SyncJobStatusDto>)response.Data ?? [];
        }

        public async Task<ApiResponse> CancelPendingJobAsync(long jobId)
        {
            return await _httpClientHelper.PostAsync(null, $"api/JobManagement/CancelPendingJob/{jobId}");
        }

        public async Task<byte[]?> GetPayloadBytesAsync(string fileName)
        {
            var response = await _httpClientHelper._httpClient.GetAsync($"{CentralizedUrlHelper.OesApiBaseUrl}api/JobManagement/DownloadPayload?{nameof(fileName)}={Uri.EscapeDataString(fileName ?? string.Empty)}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            return null;
        }

        #endregion RabbitMQJobManagment

        #region ManualSync

        public async Task<ApiResponse> ManualSyncAllVenuesAsync()
        {
            return await _httpClientHelper.PostAsync(null, "api/SyncCandidates/SyncTodayTomorrow");
        }

        public async Task<ApiResponse> SyncByVenueAndDateAsync(string venueCode, DateTime examDate)
        {
            return await _httpClientHelper.PostAsync(null, $"api/SyncCandidates/ManualSyncByVenueDate?{nameof(venueCode)}={venueCode}&{nameof(examDate)}={examDate:yyyy-MM-dd}");
        }

        #endregion ManualSync

        #region CBTSyncStatus

        public async Task<ApiResponse> GetCBTSyncStatusAsync()
        {
            return await _httpClientHelper.GetAsync<List<CBTSyncStatusResponseDto>>("api/SyncCandidates/CBTSyncStatus");
        }

        public async Task<ApiResponse> GetCentersSyncStatusAsync()
        {
            return await _httpClientHelper.GetAsync<List<CentersSyncStatusResponseDto>>("api/SyncCandidates/CentersSyncStatus");
        }

        #endregion CBTSyncStatus

        #region AutoSchedulesSync

        public async Task<ApiResponse> ToggleAutoSyncAsync(long scheduleId, bool isEnabled)
        {
            return await _httpClientHelper.PutAsync(null, $"api/Schedule/ToggleAutoSync?{nameof(scheduleId)}={scheduleId}&{nameof(isEnabled)}={isEnabled}");
        }

        public async Task<ApiResponse> GetAutoSyncStatusAsync(long scheduleId)
        {
            return await _httpClientHelper.GetAsync<AutoSyncSettingsDto>($"api/Schedule/GetAutoSyncStatus?{nameof(scheduleId)}={scheduleId}");
        }

        #endregion AutoSchedulesSync
    }
}