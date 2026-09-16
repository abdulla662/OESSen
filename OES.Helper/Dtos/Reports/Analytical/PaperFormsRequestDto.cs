
namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record PaperFormsRequestDto(
        List<long> ScheduleIds,
        List<string> PaperCodes);
}
