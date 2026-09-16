namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record PerformanceSummaryReportDto(
        string Schedule,
        string Venue,
        string PaperCode,
        DateTime? ExamDate,
        int TotalQuestions,
        int ExamTakers,
        double AverageScorePct,
        double LowScorePct,
        double HighScorePct,
        double Kr20,
        List<ScoreFrequencyBucketDto> HistogramBuckets,
        List<LearningOutcomeDto> LearningOutcomes,
        List<AtRiskStudentDto> AtRiskStudents,
        List<QuestionPerformanceDto> QuestionPerformance);

    public sealed record LearningOutcomeDto(
        string CategoryName,
        double AvgCorrectPct);

    public sealed record AtRiskStudentDto(
        string CandidateCode,
        string DisplayName,
        double RawScore,
        double ScorePct);

    public sealed record QuestionPerformanceDto(
        int SrNo,
        string QuestionCode,
        string ItemStem,
        double CorrectPct,
        double Upper27Pct,
        double Lower27Pct,
        double? PointBiserial,
        double DiscIndex,
        double FreqA,
        double FreqB,
        double FreqC,
        double FreqD,
        double FreqE,
        double FreqF);
}
