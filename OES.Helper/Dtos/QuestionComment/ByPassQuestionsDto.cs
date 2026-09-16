
namespace OES.Helper.Dtos.QuestionComment
{
    public sealed record BypassQuestionsDto(
        List<long> QuestionMetaDataIds,
        string QCAuthor
    );
}
