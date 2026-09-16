using OES.Helper.Dtos.Reports;
using OES.Helper.Dtos.Reports.Analytical;

namespace OES.Blazor.Services.Interfaces.Report
{
    public interface IBlazResultsReportsService
    {
        Task<RawScoreResultsPagedDto> GetRawScoreResultsReportAsync(RawScoreResultsReportRequestDto request);
        Task<List<FormLookupDto>> GetItemAnalysisFormsAsync(string paperCode);
        Task<List<PaperLookupDto>> GetItemAnalysisPapersAsync();
        Task<ScoreDistributionReportDto> GetScoreDistributionReportAsync(ScoreDistributionReportRequestDto request);
        Task<ExportFileDto> GetResponseMatrixReportAsync(ResponseMatrixReportRequestDto request);
        Task<ExportFileDto> GetItemAnalysisReportAsync(ItemAnalysisReportRequestDto request);
    }
}
