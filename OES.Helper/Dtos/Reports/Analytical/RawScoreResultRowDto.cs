namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record RawScoreResultRowDto(
        string? NationalId,
        string? Name,
        string? Gender,
        string? Email,
        string? PaperName,
        string? FormName,
        int TotalMarks,
        int Score,
        string ScorePct);
}
