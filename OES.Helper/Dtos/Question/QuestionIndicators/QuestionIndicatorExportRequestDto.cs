using OES.Helper.Enums;

namespace OES.Helper.Dtos.Question.QuestionIndicators
{
    public sealed record QuestionIndicatorExportRequestDto(
        int Percentage,
        DateTime? FromDate,
        DateTime? ToDate,
        IndicatorType Type
    );
}
