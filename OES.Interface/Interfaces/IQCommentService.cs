using OES.Helper.Dtos.QuestionComment;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IQCommentService
    {
        Task<IApiResponse> CreateComment(QCommentDto _commentDto);
        Task<IApiResponse> BypassQuestionsAsync(BypassQuestionsDto bypassQuestionsDto);
        Task<ApiResponse> GetQcCommentsByMetaDataId(PaginationSearchModel pagination, long metaDataId = 0);
    }
}
