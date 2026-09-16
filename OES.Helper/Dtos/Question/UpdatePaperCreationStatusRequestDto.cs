using OES.Helper.Enums;

namespace OES.Helper.Dtos.Question
{
    public sealed record UpdateQuestionCreationStatusRequestDto(long QuestionMetadataId, QuestionStatus CurrentQuestionStatus);
}
