
namespace OES.Helper.Dtos.Question.QuestionMetadataDtos
{
    public record UpdateQuestionsCreationStatusRequestDto(List<string> QuestionCodes, QuestionStatus CurrentQuestionStatus);
}
