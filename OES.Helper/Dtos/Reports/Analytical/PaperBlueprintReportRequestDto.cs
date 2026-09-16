
namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record PaperBlueprintReportRequestDto(
        List<long> ScheduleIds,
        List<string> PaperCodes,
        List<long> FormIds);
}
