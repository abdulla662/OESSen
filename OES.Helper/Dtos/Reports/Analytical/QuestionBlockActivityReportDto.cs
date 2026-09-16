using SharedHelper.Enums;

namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record QuestionBlockActivityReportDto(
        AdaptivePaperSubtype? ExamType,
        List<QuestionBlockActivityRowDto> Rows);

    public sealed record QuestionBlockActivityRowDto(
        long QuestionId,
        string QuestionCode,
        long BlockId,
        string BlockCode,
        string Status);
}
