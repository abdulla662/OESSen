
namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record ScoreDistributionReportDto(
       string PaperName,
       string FormName,
       int TotalMarks,
       double CutScore,
       int TotalCandidates,
       int PassCount,
       int FailCount,
       double PassRate,
       double Mean,
       double Median,
       double Mode,
       double StdDev,
       double Min,
       double Max,
       double Skewness,
       double Kurtosis,
       string SkewnessInterpretation,
       string KurtosisInterpretation,
       string ShapeInterpretation,
       List<ScoreDistributionBinDto> Bins,
       List<PercentileRowDto> Percentiles,
       List<ScoreBandDto> Bands);

    public sealed record ScoreDistributionBinDto(
        string Label,
        double LowScore,
        double HighScore,
        int Count,
        double Percentage,
        bool IsPassZone);

    public sealed record PercentileRowDto(
        string Label,
        double Score,
        string Meaning);

    public sealed record ScoreBandDto(
        string Band,
        int Count,
        double Percentage,
        bool ContainsCutScore,
        double BarFillPct);
}
