using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.QComment;
using OES.Helper.Dtos.QcComments;
using OES.Helper.Dtos.QuestionComment;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.QComment
{
    public class BlazQCommentService(IHttpClientHelper _httpClientHelper, IBlazGetCustomTableData<QcCommentDto> _blazGetCustomTableData) : IBlazQCommentService
    {
        public async Task<ApiResponse> AddComment(QCommentDto _qCommentDto)
        {
            return await _httpClientHelper.PostAsync(_qCommentDto, "api/QComment/AddQComment");
        }
        public async Task<CustomTableData<QcCommentDto>> GetCommentsByMetaDataId(PaginationSearchModel paginationSearch, long QuestionMetaDataId)
        {
            return await _blazGetCustomTableData.GetCustomTableData(paginationSearch, $"api/QComment/GetQcCommentsByMetaDataId?QuestionMetaDataId={QuestionMetaDataId}");
        }
    }
}
