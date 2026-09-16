using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class QuestionLayoutController(IQuestionLayoutService _questionLayoutService) : OESBaseController
    {
        [OESFilter(Authorize = true,ApplySignatureFilter = false, ApplyOrganizationIdFilter =false)]
        [HttpGet("GetLayoutsByQuestionTypeId")]
        public async Task<ApiResponse> GetLayoutsByQuestionTypeId(long questionTypeId)
        {
            return await _questionLayoutService.GetAllLayoutsByQuestionTypeId(questionTypeId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("AssignLayoutToQuestionMetadata")]
        public async Task<ApiResponse> AssignLayoutToQuestionMetadataAsync(long questionMetadataId, long layoutId)
        {
            return await _questionLayoutService.AssignLayoutToQuestionMetadataAsync(questionMetadataId, layoutId);
        }
    }
}
