using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.CandidatesResult;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.Results;
using OES.Helper.Dtos.TrackingLog;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.CandidatesResult
{
    public interface IBlazCandidatesResultService
    {
        Task<CustomTableData<GetCandidateQuestionsAnswersDto>> GetAllCandidateQuestionsAnswersAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> AddCandidateQuestionsAnswersAsync(List<AddCandidateQuestionsAnswersDto> candidateQuestionsAnswersDtos);

        Task<ApiResponse> GetCandidateResultByIdAsync(long id);

        Task<ApiResponse> SaveCombinedFileToCTRAsync(SaveFileToCTRDto dto);

        Task<ApiResponse> MarkExportsAsSentToCTRAsync(List<ExportCandidatesRequestDto> requests);

        Task<ApiResponse> AddCandidateExamDetailsAsync(List<AddCandidateExamDetailsDto> candidateExamDetailsDtos);

        Task<CustomTableData<GetCandidateExamDetailsDto>> GetUnfinishedCandidatesForReviewAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> UpdateReviewStatusAsync(UpdateReviewStatusDto updateReviewStatusDto);

        Task<ApiResponse> ExportUnfinishedReviewAsync(ExportUnfinishedReviewRequestDto request);

        Task<ApiResponse> GetAttendanceReportsByDateRangeAsync(DateTime startDate, DateTime endDate);

        Task<ApiResponse> GetAllAttendanceReportsAsync();

        Task<ApiResponse> GetAttendanceReportByPaperFormAsync(long paperFormId);

        Task<ApiResponse> GetAttendanceReportsByScheduleAsync(long scheduleId);

        Task<ApiResponse> AddCTRExamSyncJobsAsync(ExportCandidatesRequestDto request);

        Task<ApiResponse> GetCTRExamSyncJobsAsync(ExportCandidatesRequestDto request);

        Task<ApiResponse> GetAllCTRExamSyncJobsForDateRangeAsync(DateOnly startDate, DateOnly endDate);

        Task<ApiResponse> GetSyncStatusOESToCESReportAsync();

        Task<ApiResponse> GetSyncStatusCESToOESReportAsync();

        Task<ApiResponse> GetTrackingLogsByRequestAsync(GetTrackingLogRequestDto request);
    }
}