using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.QuestionComment;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class QCommentController(IQCommentService _qCommentService) : OESBaseController
    {
        [HttpPost("AddQComment")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddQComment(QCommentDto _qCommentDto)
        {
            return await _qCommentService.CreateComment(_qCommentDto);
        }

        [HttpPost("BypassQuestions")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> BypassQuestionsAsync(BypassQuestionsDto bypassQuestionsDto)
        {
            return await _qCommentService.BypassQuestionsAsync(bypassQuestionsDto);
        }

        [HttpPost("GetQcCommentsByMetaDataId")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetQcCommentsByMetaDataId(PaginationSearchModel paginationModel, long QuestionMetaDataId)
        {
            return await _qCommentService.GetQcCommentsByMetaDataId(paginationModel, QuestionMetaDataId);
        }
    }
}
