using OES.Helper.General;

namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record ResponseMatrixReportRequestDto(
        List<long> FormIds,
        PaginationSearchModel Pagination);
}
