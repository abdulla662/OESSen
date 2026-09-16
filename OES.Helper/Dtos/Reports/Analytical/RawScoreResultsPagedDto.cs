namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record RawScoreResultsPagedDto(
        int TotalCount,
        List<RawScoreResultRowDto> Rows,
        string PaperName,
        List<string> FormNames);
}
