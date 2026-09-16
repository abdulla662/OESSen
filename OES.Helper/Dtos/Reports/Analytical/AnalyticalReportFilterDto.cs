namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record AnalyticalReportFilterDto
    {
        public List<long> ScheduleIds { get; set; } = [];
        public List<string> VenueCodes { get; set; } = [];
        public List<string> PaperCodes { get; set; } = [];
        public List<long> FormIds { get; set; } = [];
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
