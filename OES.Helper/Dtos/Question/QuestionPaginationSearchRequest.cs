using OES.Helper.General;

namespace OES.Helper.Dtos.Question
{
    public sealed record QuestionPaginationSearchRequest(PaginationSearchModel PaginationSearch, bool IsFromQuestionAI);
}
