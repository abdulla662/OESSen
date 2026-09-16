using OES.Helper.Dtos.QcComments;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.QuestionComment;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.QComment
{
    public interface IBlazQCommentService
    {
        Task<ApiResponse> AddComment(QCommentDto _qCommentDto);
        Task<CustomTableData<QcCommentDto>> GetCommentsByMetaDataId(PaginationSearchModel paginationSearch,long QuestionMetaDataId);

    }
}
