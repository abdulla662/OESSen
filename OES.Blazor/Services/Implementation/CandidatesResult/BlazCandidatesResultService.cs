using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.CandidatesResult;
using OES.Blazor.Services.Interfaces.Common;
using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.CandidatesResult;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.Results;
using OES.Helper.Dtos.TrackingLog;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.CandidatesResult
{
    public class BlazCandidatesResultService : IBlazCandidatesResultService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IBlazGetCustomTableData<GetCandidateQuestionsAnswersDto> _blazGetCustomTableData;
        private readonly IBlazGetCustomTableData<GetCandidateExamDetailsDto> _blazGetCandidateExamDetailsCustomTableData;

        public BlazCandidatesResultService(
            IHttpClientHelper httpClientHelper,
            IBlazGetCustomTableData<GetCandidateQuestionsAnswersDto> blazGetCustomTableData,
            IBlazGetCustomTableData<GetCandidateExamDetailsDto> blazGetCandidateExamDetailsCustomTableData
        )
        {
            _httpClientHelper = httpClientHelper;
            _blazGetCustomTableData = blazGetCustomTableData;
            _blazGetCandidateExamDetailsCustomTableData = blazGetCandidateExamDetailsCustomTableData;
        }

        public async Task<CustomTableData<GetCandidateQuestionsAnswersDto>> GetAllCandidateQuestionsAnswersAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _blazGetCustomTableData.GetCustomTableData(paginationSearchModel, "api/CandidatesResult/GetAllCandidateQuestionsAnswers");
        }

        public async Task<ApiResponse> AddCandidateQuestionsAnswersAsync(List<AddCandidateQuestionsAnswersDto> candidateQuestionsAnswersDtos)
        {
            return await _httpClientHelper.PostAsync(candidateQuestionsAnswersDtos, "api/CandidatesResult/AddCandidatesQuestionsAnswers");
        }

        public async Task<ApiResponse> SaveCombinedFileToCTRAsync(SaveFileToCTRDto dto)
        {
            return await _httpClientHelper.PostAsync(dto, "api/CandidatesResult/SaveCombinedFileToCTRAsync");
        }

        public async Task<ApiResponse> MarkExportsAsSentToCTRAsync(List<ExportCandidatesRequestDto> requests)
        {
            return await _httpClientHelper.PostAsync(requests, "api/CandidatesResult/MarkExportsAsSentToCTRAsync");
        }

        public async Task<ApiResponse> GetCandidateResultByIdAsync(long id)
        {
            return await _httpClientHelper.GetAsync<GetCandidateQuestionsAnswersDto>($"api/CandidatesResult/GetCandidateResultById?{nameof(id)}={id}");
        }

        public async Task<ApiResponse> AddCandidateExamDetailsAsync(List<AddCandidateExamDetailsDto> candidateExamDetailsDtos)
        {
            return await _httpClientHelper.PostAsync(candidateExamDetailsDtos, "api/CandidatesResult/AddCandidateExamDetails");
        }

        public async Task<CustomTableData<GetCandidateExamDetailsDto>> GetUnfinishedCandidatesForReviewAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _blazGetCandidateExamDetailsCustomTableData.GetCustomTableData(paginationSearchModel, "api/CandidatesResult/GetUnfinishedCandidatesForReview");
        }

        public async Task<ApiResponse> GetAttendanceReportsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _httpClientHelper.GetAsync<AttendanceReportGroupedDto>($"api/CandidatesResult/GetAttendanceReportsByDateRange?{nameof(startDate)}={startDate:yyyy-MM-dd}&{nameof(endDate)}={endDate:yyyy-MM-dd}");
        }

        public async Task<ApiResponse> GetAllAttendanceReportsAsync()
        {
            return await _httpClientHelper.GetAsync<List<PaperFormAttendanceReportDto>>("api/CandidatesResult/GetAllAttendanceReports");
        }

        public async Task<ApiResponse> GetAttendanceReportByPaperFormAsync(long paperFormId)
        {
            return await _httpClientHelper.GetAsync<PaperFormAttendanceReportDto>($"api/CandidatesResult/GetAttendanceReportByPaperForm?{nameof(paperFormId)}={paperFormId}");
        }

        public async Task<ApiResponse> GetAttendanceReportsByScheduleAsync(long scheduleId)
        {
            return await _httpClientHelper.GetAsync<List<PaperFormAttendanceReportDto>>($"api/CandidatesResult/GetAttendanceReportsBySchedule?{nameof(scheduleId)}={scheduleId}");
        }

        public async Task<ApiResponse> UpdateReviewStatusAsync(UpdateReviewStatusDto updateReviewStatusDto)
        {
            return await _httpClientHelper.PutAsync(updateReviewStatusDto, "api/CandidatesResult/UpdateReviewStatus");
        }

        public async Task<ApiResponse> ExportUnfinishedReviewAsync(ExportUnfinishedReviewRequestDto request)
        {
            return await _httpClientHelper.PostAsync(request, "api/CandidatesResult/ExportUnfinishedReview");
        }

        public async Task<ApiResponse> AddCTRExamSyncJobsAsync(ExportCandidatesRequestDto request)
        {
            return await _httpClientHelper.PostAsync(request, "api/CandidatesResult/AddCTRExamSyncJobs");
        }

        public async Task<ApiResponse> GetCTRExamSyncJobsAsync(ExportCandidatesRequestDto request)
        {
            return await _httpClientHelper.PostAsync(request, "api/CandidatesResult/GetCTRExamSyncJobs");
        }

        public async Task<ApiResponse> GetAllCTRExamSyncJobsForDateRangeAsync(DateOnly startDate, DateOnly endDate)
        {
            return await _httpClientHelper.GetAsync<object>(
                $"api/CandidatesResult/GetAllCTRExamSyncJobsForDateRange?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        }

        public async Task<ApiResponse> GetSyncStatusOESToCESReportAsync()
        {
            return await _httpClientHelper.GetAsync<List<SyncStatusOESToCESReportDto>>("api/CandidatesResult/GetSyncStatusOESToCESReport");
        }

        public async Task<ApiResponse> GetSyncStatusCESToOESReportAsync()
        {
            return await _httpClientHelper.GetAsync<List<SyncStatusCESToOESReportDto>>("api/CandidatesResult/GetSyncStatusCESToOESReport");
        }

        public async Task<ApiResponse> GetTrackingLogsByRequestAsync(GetTrackingLogRequestDto request)
        {
            return await _httpClientHelper.PostAsync(request, "api/CandidatesResult/GetTrackingLogs");
        }
    }
}