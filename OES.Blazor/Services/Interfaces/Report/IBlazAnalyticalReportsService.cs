using OES.Helper.Dtos.Reports.Analytical;

namespace OES.Blazor.Services.Interfaces.Report
{
    public interface IBlazAnalyticalReportsService
    {
        Task<List<ScheduleLookupDto>> GetSchedulesAsync();
        Task<List<VenueLookupDto>> GetVenuesAsync(List<long> scheduleIds);
        Task<List<PaperCodeLookupDto>> GetPapersAsync(List<long> scheduleIds);
        Task<List<ItemBankLookupDto>> GetItemBanksAsync();
        Task<List<PaperFormLookupDto>> GetFormsAsync(PaperFormsRequestDto paperFormsRequestDto);
        Task<CondensedTestReportDto> GetCondensedTestReportAsync(CondensedTestReportRequestDto condensedTestReportRequestDto);
        Task<ItemTypeReportDto> GetItemTypeReportAsync(long? itemBankId);
        Task<PaperBlueprintReportDto> GetPaperBlueprintReportAsync(PaperBlueprintReportRequestDto request);
        Task<PerformanceSummaryReportDto> GetPerformanceSummaryReportAsync(PerformanceSummaryReportRequestDto request);
        Task<QuestionUsageReportDto> GetQuestionUsageReportAsync(QuestionUsageReportRequestDto request);
        Task<TestAnalysisReportDto> GetTestAnalysisReportAsync(TestAnalysisReportRequestDto request);
        Task<QuestionBlockActivityReportDto> GetQuestionBlockActivityReportAsync(QuestionBlockActivityReportRequestDto request);
    }
}
