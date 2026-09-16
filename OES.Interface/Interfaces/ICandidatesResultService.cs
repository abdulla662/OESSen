using OES.Helper.Dtos.CandidatesResult;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.Results;
using OES.Helper.Dtos.TrackingLog;
using OES.Helper.General;
using OES.Helper.Dtos.Candidate;
using SharedHelper.Dtos;

namespace OES.Interface.Interfaces
{
    public interface ICandidatesResultService
    {
        Task<ApiResponse> GetAllCandidateQuestionsAnswersAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> AddCandidatesQuestionsAnswersAsync(List<AddCandidateQuestionsAnswersDto> candidateQuestionsAnswersDtos);

        Task<ApiResponse> AddTrackingLogsAsync(List<AddTrackingLogDto> trackingLogDtos);

        Task<ApiResponse> GetTrackingLogsByRequestAsync(GetTrackingLogRequestDto request);

        Task<ApiResponse> GetCandidateResultByIdAsync(long id);

        Task<ApiResponse> AddCandidateExamDetailsAsync(List<AddCandidateExamDetailsDto> candidateExamDetailsDtos);

        Task<ApiResponse> GetUnfinishedCandidatesForReviewAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> UpdateReviewStatusAsync(UpdateReviewStatusDto updateReviewStatusDto);

        Task<ApiResponse> ExportUnfinishedReviewAsync(ExportUnfinishedReviewRequestDto request);

        Task<ApiResponse> GetAttendanceReportsByDateRangeAsync(DateOnly startDate, DateOnly endDate);

        Task<ApiResponse> GetAllAttendanceReportsAsync();

        Task<ApiResponse> GetAttendanceReportByPaperFormAsync(long paperFormId);

        Task<ApiResponse> GetAttendanceReportsByScheduleAsync(long scheduleId);

        Task<ApiResponse> AddCTRExamSyncJobsAsync(ExportCandidatesRequestDto request);

        Task<ApiResponse> GetCTRExamSyncJobsAsync(ExportCandidatesRequestDto request);

        Task<ApiResponse>  SaveCombinedFileToCTRAsync(SaveFileToCTRDto dto);

        Task<ApiResponse> MarkExportsAsSentToCTRAsync(List<ExportCandidatesRequestDto> requests);

        /// <summary>
        /// Fetches all CTR sync jobs for a date range in a single query.
        /// Used for batch operations to avoid N+1 API calls.
        /// </summary>
        Task<ApiResponse> GetAllCTRExamSyncJobsForDateRangeAsync(DateOnly startDate, DateOnly endDate);

        Task<ApiResponse> AddBlockCandidateAnswersAsync(List<AddBlockCandidateAnswerDto> blockCandidateAnswerDtos);

        Task<ApiResponse> GetSyncStatusOESToCESReportAsync();

        Task<ApiResponse> GetSyncStatusCESToOESReportAsync();

        Task<ApiResponse> UpdateEvaluationScoresAsync(List<CandidateAnswersScoresDto> scores);
    }
}