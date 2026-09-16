
namespace OES.Helper.Dtos.Question.QuestionMetadataDtos
{
    public sealed record UpdateQuestionDeltaDto(
        int RowNumber,
        string QuestionCode,
        decimal? DeltaValue
    );
}
