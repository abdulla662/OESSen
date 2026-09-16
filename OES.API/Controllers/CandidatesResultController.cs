using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.CandidatesResult;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.Results;
using OES.Helper.Dtos.TrackingLog;
using OES.Helper.General;
using OES.Interface.Interfaces;
using SharedHelper.Dtos;

namespace OES.API.Controllers
{
    public class CandidatesResultController : OESBaseController, ICandidatesResultService
    {
        private readonly ICandidatesResultService _candidatesResultService;

        public CandidatesResultController(ICandidatesResultService candidatesResultService)
        {
            _candidatesResultService = candidatesResultService;
        }

        [HttpPost("GetAllCandidateQuestionsAnswers")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllCandidateQuestionsAnswersAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _candidatesResultService.GetAllCandidateQuestionsAnswersAsync(paginationSearchModel);
        }

        [HttpPost("AddCandidatesQuestionsAnswers")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddCandidatesQuestionsAnswersAsync(List<AddCandidateQuestionsAnswersDto> candidateQuestionsAnswersDtos)
        {
            return await _candidatesResultService.AddCandidatesQuestionsAnswersAsync(candidateQuestionsAnswersDtos);
        }

        [HttpPost("AddCandidateExamDetails")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddCandidateExamDetailsAsync(List<AddCandidateExamDetailsDto> candidateExamDetailsDto)
        {
            return await _candidatesResultService.AddCandidateExamDetailsAsync(candidateExamDetailsDto);
        }

        [HttpGet("GetCandidateResultById")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetCandidateResultByIdAsync(long id)
        {
            return await _candidatesResultService.GetCandidateResultByIdAsync(id);
        }

        [HttpPost("GetUnfinishedCandidatesForReview")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetUnfinishedCandidatesForReviewAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _candidatesResultService.GetUnfinishedCandidatesForReviewAsync(paginationSearchModel);
        }

        [HttpGet("GetAttendanceReportsByDateRange")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAttendanceReportsByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        {
            return await _candidatesResultService.GetAttendanceReportsByDateRangeAsync(startDate, endDate);
        }

        [HttpGet("GetAllAttendanceReports")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllAttendanceReportsAsync()
        {
            return await _candidatesResultService.GetAllAttendanceReportsAsync();
        }

        [HttpGet("GetAttendanceReportByPaperForm")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAttendanceReportByPaperFormAsync(long paperFormId)
        {
            return await _candidatesResultService.GetAttendanceReportByPaperFormAsync(paperFormId);
        }

        [HttpGet("GetAttendanceReportsBySchedule")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAttendanceReportsByScheduleAsync(long scheduleId)
        {
            return await _candidatesResultService.GetAttendanceReportsByScheduleAsync(scheduleId);
        }

        [HttpPut("UpdateReviewStatus")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateReviewStatusAsync([FromBody] UpdateReviewStatusDto updateReviewStatusDto)
        {
            return await _candidatesResultService.UpdateReviewStatusAsync(updateReviewStatusDto);
        }

        [HttpPost("ExportUnfinishedReview")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> ExportUnfinishedReviewAsync([FromBody] ExportUnfinishedReviewRequestDto request)
        {
            return await _candidatesResultService.ExportUnfinishedReviewAsync(request);
        }

        [HttpPost("AddCTRExamSyncJobs")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddCTRExamSyncJobsAsync([FromBody] ExportCandidatesRequestDto request)
        {
            return await _candidatesResultService.AddCTRExamSyncJobsAsync(request);
        }

        [HttpPost("SaveCombinedFileToCTRAsync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> SaveCombinedFileToCTRAsync([FromBody] SaveFileToCTRDto dto)
        {
            return await _candidatesResultService.SaveCombinedFileToCTRAsync(dto);
        }

        [HttpPost("MarkExportsAsSentToCTRAsync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> MarkExportsAsSentToCTRAsync([FromBody] List<ExportCandidatesRequestDto> requests)
        {
            return await _candidatesResultService.MarkExportsAsSentToCTRAsync(requests);
        }

        [HttpPost("GetCTRExamSyncJobs")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetCTRExamSyncJobsAsync(ExportCandidatesRequestDto request)
        {
            return await _candidatesResultService.GetCTRExamSyncJobsAsync(request);
        }

        [HttpGet("GetAllCTRExamSyncJobsForDateRange")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllCTRExamSyncJobsForDateRangeAsync([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate)
        {
            return await _candidatesResultService.GetAllCTRExamSyncJobsForDateRangeAsync(startDate, endDate);
        }

        [HttpPost("AddBlockCandidateAnswers")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddBlockCandidateAnswersAsync(List<AddBlockCandidateAnswerDto> blockCandidateAnswerDtos)
        {
            return await _candidatesResultService.AddBlockCandidateAnswersAsync(blockCandidateAnswerDtos);
        }

        [HttpPost("AddTrackingLogs")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddTrackingLogsAsync(List<AddTrackingLogDto> addTrackingLogDtos)
        {
            return await _candidatesResultService.AddTrackingLogsAsync(addTrackingLogDtos);
        }

        [HttpPost("GetTrackingLogs")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetTrackingLogsByRequestAsync([FromBody] GetTrackingLogRequestDto request)
        {
            return await _candidatesResultService.GetTrackingLogsByRequestAsync(request);
        }

        [HttpGet("GetSyncStatusOESToCESReport")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetSyncStatusOESToCESReportAsync()
        {
            return await _candidatesResultService.GetSyncStatusOESToCESReportAsync();
        }

        [HttpGet("GetSyncStatusCESToOESReport")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetSyncStatusCESToOESReportAsync()
        {
            return await _candidatesResultService.GetSyncStatusCESToOESReportAsync();
        }

        [HttpPost("UpdateEvaluationScores")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateEvaluationScoresAsync(List<CandidateAnswersScoresDto> scores)
        {
            return await _candidatesResultService.UpdateEvaluationScoresAsync(scores);
        }
    }
}