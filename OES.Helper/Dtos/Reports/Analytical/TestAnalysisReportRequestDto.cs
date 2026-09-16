
namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record TestAnalysisReportRequestDto(
        List<long> ScheduleIds,
        List<string> PaperCodes,
        List<string> VenueCodes);
}
