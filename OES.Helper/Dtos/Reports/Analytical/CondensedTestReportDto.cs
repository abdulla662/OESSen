namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record CondensedTestReportDto
    {
        public string Schedule { get; set; }
        public string Venue { get; set; }
        public string PaperCode { get; set; }
        public DateTime? ExamDate { get; set; }
        public int TotalPossiblePoints { get; set; }
        public int TotalStudents { get; set; }
        public double MedianScore { get; set; }
        public double MeanScore { get; set; }
        public double MaxScore { get; set; }
        public double MinScore { get; set; }
        public double StdDev { get; set; }
        public double Kr20 { get; set; }
        public double RangeOfScores { get; set; }
        public List<CondensedTestItemDto> Items { get; set; } = [];
    }

    public sealed record CondensedTestItemDto
    {
        public int SrNo { get; set; }
        public string QuestionCode { get; set; }
        public string CorrectAnswer { get; set; }
        public double FreqA { get; set; }
        public double FreqB { get; set; }
        public double FreqC { get; set; }
        public double FreqD { get; set; }
        public double FreqE { get; set; }
        public double FreqF { get; set; }
        public string NonDistractors { get; set; }
        public double CorrectPct { get; set; }
        public double Upper27Pct { get; set; }
        public double Lower27Pct { get; set; }
        public double? PointBiserial { get; set; }
        public bool HasDistractorAboveCorrect { get; set; }
    }
}
