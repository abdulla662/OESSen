using OES.Helper.Enums;

namespace OES.Helper.Dtos.Question.QuestionMetadataDtos
{
    public sealed record QuestionDeltaRowResultDto(
        int RowNumber,
        string QuestionCode,
        decimal? DeltaValue,
        QuestionDeltaRowStatus Status,
        string? Message
    );
}
