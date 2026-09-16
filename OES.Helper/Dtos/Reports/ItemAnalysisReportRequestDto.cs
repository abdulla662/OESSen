using OES.Helper.General;

namespace OES.Helper.Dtos.Reports
{
    public sealed record ItemAnalysisReportRequestDto(
        long PaperId,
        List<long> FormIds,
        bool SplitByGender,
        int GenderMaleValue,
        int GenderFemaleValue,
        PaginationSearchModel Pagination);
}

