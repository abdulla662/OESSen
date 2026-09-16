
namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record PerformanceSummaryReportRequestDto(
        List<long> ScheduleIds,
        List<string> VenueCodes,
        List<string> PaperCodes);
}
