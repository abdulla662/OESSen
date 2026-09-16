
namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record CondensedTestReportRequestDto
    {
        public List<long> ScheduleIds { get; init; } = [];

        public List<string> PaperCodes { get; init; } = [];

        public List<string> VenueCodes { get; init; } = [];
    }
}
