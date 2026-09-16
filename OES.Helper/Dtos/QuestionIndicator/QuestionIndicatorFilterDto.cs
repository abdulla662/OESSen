using OES.Helper.Enums;

namespace OES.Helper.Dtos.QuestionIndicator
{
    public sealed record QuestionIndicatorFilterDto
    {
       public int Percentage { get; init; }
       public IndicatorType Type { get; init; }
       public DateTime? FromDate { get; init; }
       public DateTime? ToDate { get; init; }
    }
}
