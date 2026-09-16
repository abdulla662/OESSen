using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Reports;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class ResultsReportsController : OESBaseController
    {
        private readonly IResultsReportsService _resultsReportsService;

        public ResultsReportsController(IResultsReportsService resultsReportsService)
        {
            _resultsReportsService = resultsReportsService;
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetRawScoreResultsReport")]
        public async Task<ApiResponse> GetRawScoreResultsReportAsync([FromBody] RawScoreResultsReportRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _resultsReportsService.GetRawScoreResultsReportAsync(request, cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetItemAnalysisReport")]
        public async Task<ApiResponse> GetItemAnalysisReportAsync([FromBody] ItemAnalysisReportRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _resultsReportsService.GetItemAnalysisReportAsync(request, cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetItemAnalysisPapers")]
        public async Task<ApiResponse> GetItemAnalysisPapersAsync(CancellationToken cancellationToken = default)
        {
            return await _resultsReportsService.GetItemAnalysisPapersAsync(cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetItemAnalysisForms")]
        public async Task<ApiResponse> GetItemAnalysisFormsAsync(string paperCode, CancellationToken cancellationToken = default)
        {
            return await _resultsReportsService.GetItemAnalysisFormsAsync(paperCode, cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetScoreDistributionReport")]
        public async Task<ApiResponse> GetScoreDistributionReportAsync([FromBody] ScoreDistributionReportRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _resultsReportsService.GetScoreDistributionReportAsync(request, cancellationToken);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetResponseMatrixReport")]
        public async Task<ApiResponse> GetResponseMatrixReportAsync([FromBody] ResponseMatrixReportRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _resultsReportsService.GetResponseMatrixReportAsync(request, cancellationToken);
        }
    }
}
