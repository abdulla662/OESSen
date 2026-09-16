namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record TestAnalysisReportDto(
        string Schedule,
        string Venue,
        string PaperCode,
        List<ScoreFrequencyBucketDto> FrequencyBuckets,
        TestAnalysisStatsDto Stats);

    public sealed record ScoreFrequencyBucketDto(
        string PercentScoreRange,
        string RawScoreRange,
        int Frequency,
        double Percent);

    public sealed record TestAnalysisStatsDto(
        int NoOfCandidates,
        int NoOfItems,
        double MinScore,
        double MaxScore,
        double Mean,
        double Median,
        double Mode,
        double StdDev,
        double Variance,
        double? CronbachAlpha,
        double StdErrorOfMean,
        double? StdErrorOfMeasurement,
        double Skew,
        double Kurtosis);
}
