using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class QuestionMetadataController(IQuestionMetadataService _questionMetadataService) : OESBaseController
    {
        [HttpPost("PaginatedSubQuestions")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPaginatedSubQuestions(long parentId, PaginationSearchModel pagination)
        {
            return await _questionMetadataService.GetPaginatedSubQuestions(parentId, pagination);
        }


        [HttpGet("GetQuestionMetaDataForQCView")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetQuestionMetaDataForQCView(long questionId)
        {
            return await _questionMetadataService.GetQuestionMetaDataForQCView(questionId);
        }


        [HttpGet("CheckIfQuestionMetadataHasAnyQuestionDetails")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> CheckIfQuestionMetadataHasAnyQuestionDetailsAsync(long questionMetadataId)
        {
            return await _questionMetadataService.CheckIfQuestionMetadataHasAnyQuestionDetailsAsync(questionMetadataId);
        }


        [HttpGet("IsTheDocumentUsedInAnyQuestion")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> IsTheDocumentUsedInAnyQuestionAsync(Guid documentId)
        {
            return await _questionMetadataService.IsTheDocumentUsedInAnyQuestionAsync(documentId);
        }


        [HttpGet("CopyOldQuestionDeeplyAsync")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> CopyOldQuestionDeeplyAsync(long questionMetadataId)
        {
            return await _questionMetadataService.CopyOldQuestionDeeplyAsync(questionMetadataId);
        }


        [HttpGet("GetQuestionVersions")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetQuestionVersionsAsync(long questionMetadataId)
        {
            return await _questionMetadataService.GetQuestionVersionsAsync(questionMetadataId);
        }

        [HttpPost("ValidateQuestionDeltaFromExcel")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> ValidateQuestionDeltaFromExcelAsync([FromForm] UpdateQuestionDeltaRequestDto updateQuestionDeltaRequestDto)
        {
            return await _questionMetadataService.ValidateQuestionDeltaFromExcelAsync(updateQuestionDeltaRequestDto);
        }

        [HttpPost("UpdateQuestionDeltaFromExcel")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UpdateQuestionDeltaAsync(UpdateQuestionDeltaConfirmDto updateQuestionDeltaConfirmDto)
        {
            return await _questionMetadataService.UpdateQuestionDeltaAsync(updateQuestionDeltaConfirmDto);
        }
    }
}
