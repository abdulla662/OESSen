namespace OES.Helper.Dtos.Reports
{
    public sealed record ItemAnalysisReportRequest
    {
        public long PaperId { get; set; }
        public long FormId { get; set; }
        public string? FormCode { get; set; }
        public bool SplitByGender { get; set; } = true;
        public int GenderMaleValue { get; set; } = 0; // DB: false/0 = Male
        public int GenderFemaleValue { get; set; } = 1; // DB: true/1  = Female
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
