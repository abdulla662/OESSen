using OES.Helper.General;

namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record ScoreDistributionReportRequestDto(
        List<long> FormIds,
        double CutScore,
        PaginationSearchModel Pagination);
}
