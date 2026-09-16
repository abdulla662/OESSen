using OES.Helper.General;

namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record RawScoreResultsReportRequestDto(
        List<long> FormIds,
        PaginationSearchModel Pagination);
}
