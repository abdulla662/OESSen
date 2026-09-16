using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class AnalyticalReportsController : OESBaseController
    {
        private readonly IAnalyticalReportsService _analyticalReportsService;

        public AnalyticalReportsController(IAnalyticalReportsService analyticalReportsService)
        {
            _analyticalReportsService = analyticalReportsService;
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetSchedules")]
        public async Task<ApiResponse> GetSchedulesAsync(CancellationToken cancellationToken = default)
        {
            return await _analyticalReportsService.GetSchedulesAsync(cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetForms")]
        public async Task<ApiResponse> GetFormsAsync([FromBody] PaperFormsRequestDto paperFormsRequestDto, CancellationToken cancellationToken = default)
        {
            return await _analyticalReportsService.GetFormsAsync(paperFormsRequestDto, cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetItemBanks")]
        public async Task<ApiResponse> GetItemBanksAsync(CancellationToken cancellationToken = default)
        {
            return await _analyticalReportsService.GetItemBanksAsync(cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetPapers")]
        public async Task<ApiResponse> GetPapersAsync([FromQuery] List<long> scheduleIds, CancellationToken cancellationToken = default)
        {
            return await _analyticalReportsService.GetPapersAsync(scheduleIds, cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetVenues")]
        public async Task<ApiResponse> GetVenuesAsync([FromQuery] List<long> scheduleIds, CancellationToken cancellationToken = default)
        {
            return await _analyticalReportsService.GetVenuesAsync(scheduleIds, cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetCondensedTestReport")]
        public async Task<ApiResponse> GetCondensedTestReportAsync([FromBody] CondensedTestReportRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _analyticalReportsService.GetCondensedTestReportAsync(request, cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetItemTypeReport")]
        public async Task<ApiResponse> GetItemTypeReportAsync(long? itemBankId, CancellationToken cancellationToken = default)
        {
            return await _analyticalReportsService.GetItemTypeReportAsync(itemBankId, cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetPaperBlueprintReport")]
        public async Task<ApiResponse> GetPaperBlueprintReportAsync([FromBody] PaperBlueprintReportRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _analyticalReportsService.GetPaperBlueprintReportAsync(request, cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetPerformanceSummaryReport")]
        public async Task<ApiResponse> GetPerformanceSummaryReportAsync([FromBody] PerformanceSummaryReportRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _analyticalReportsService.GetPerformanceSummaryReportAsync(request, cancellationToken);
        }

        [HttpPost("GetQuestionUsageReport")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetQuestionUsageReportAsync([FromBody] QuestionUsageReportRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _analyticalReportsService.GetQuestionUsageReportAsync(request, cancellationToken);
        }

        [HttpPost("GetTestAnalysisReport")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetTestAnalysisReportAsync([FromBody] TestAnalysisReportRequestDto request, CancellationToken cancellationToken)
        {
            return await _analyticalReportsService.GetTestAnalysisReportAsync(request, cancellationToken);
        }

        [HttpPost("GetQuestionBlockActivityReport")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetQuestionBlockActivityReportAsync([FromBody] QuestionBlockActivityReportRequestDto request, CancellationToken cancellationToken)
        {
            return await _analyticalReportsService.GetQuestionBlockActivityReportAsync(request, cancellationToken);
        }
    }
}
