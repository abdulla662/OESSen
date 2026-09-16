using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.QuestionUploadTemplateDto.OES.Helper.Dtos.UploadFiles;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class QuestionUploadTemplateController : OESBaseController
    {
        private readonly IQuestionUploadTemplateService _service;

        public QuestionUploadTemplateController(IQuestionUploadTemplateService service)
        {
            _service = service;
        }

        [HttpPost("getAllPaginated")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllPaginatedTemplatesAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _service.GetAllPaginatedTemplatesAsync(paginationSearchModel);
        }

        [HttpGet("getById")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetByIdAsync(long templateId)
        {
            return await _service.GetByIdAsync(templateId);
        }

        [HttpPost("add")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddTemplateAsync(QuestionUploadTemplateDto questionUploadTemplateDto)
        {
            return await _service.AddTemplateAsync(questionUploadTemplateDto);
        }

        [HttpDelete("delete")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DeleteTemplateAsync(long templateId)
        {
            return await _service.DeleteTemplateAsync(templateId);
        }
    }
}

