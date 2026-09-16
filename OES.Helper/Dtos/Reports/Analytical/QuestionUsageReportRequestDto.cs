
namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record QuestionUsageReportRequestDto(
        List<long> ScheduleIds,
        List<string> VenueCodes,
        List<string> PaperCodes);
}
