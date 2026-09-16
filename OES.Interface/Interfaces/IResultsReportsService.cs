
using OES.Helper.Dtos.Reports;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IResultsReportsService
    {
        Task<ApiResponse> GetRawScoreResultsReportAsync(RawScoreResultsReportRequestDto request, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetItemAnalysisPapersAsync(CancellationToken cancellationToken = default);
        Task<ApiResponse> GetItemAnalysisFormsAsync(string paperCode, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetScoreDistributionReportAsync(ScoreDistributionReportRequestDto request, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetResponseMatrixReportAsync(ResponseMatrixReportRequestDto request, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetItemAnalysisReportAsync(ItemAnalysisReportRequestDto request, CancellationToken cancellationToken = default);
    }
}
