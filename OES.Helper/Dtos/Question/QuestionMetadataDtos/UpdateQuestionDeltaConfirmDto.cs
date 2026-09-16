
namespace OES.Helper.Dtos.Question.QuestionMetadataDtos
{
    public sealed record UpdateQuestionDeltaConfirmDto(
        List<UpdateQuestionDelta> UpdateQuestionDeltas
    );

    public sealed record UpdateQuestionDelta(
        string QuestionCode,
        decimal? DeltaValue
    );
}
