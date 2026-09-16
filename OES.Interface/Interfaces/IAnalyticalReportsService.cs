using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IAnalyticalReportsService
    {
        Task<ApiResponse> GetSchedulesAsync(CancellationToken cancellationToken = default);
        Task<ApiResponse> GetVenuesAsync(List<long> scheduleIds, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetPapersAsync(List<long> scheduleIds, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetFormsAsync(PaperFormsRequestDto paperFormsRequestDto, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetItemBanksAsync(CancellationToken cancellationToken = default);
        Task<ApiResponse> GetCondensedTestReportAsync(CondensedTestReportRequestDto request, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetItemTypeReportAsync(long? itemBankId, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetPaperBlueprintReportAsync(PaperBlueprintReportRequestDto request, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetPerformanceSummaryReportAsync(PerformanceSummaryReportRequestDto request, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetQuestionUsageReportAsync(QuestionUsageReportRequestDto request, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetTestAnalysisReportAsync(TestAnalysisReportRequestDto request, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetQuestionBlockActivityReportAsync(QuestionBlockActivityReportRequestDto request, CancellationToken cancellationToken = default);
    };
}
