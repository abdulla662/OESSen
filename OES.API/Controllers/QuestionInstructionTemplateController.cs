using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class QuestionInstructionTemplateController : OESBaseController
    {
        private readonly IQuestionInstructionTemplateService _instructionTemplateService;

        public QuestionInstructionTemplateController(IQuestionInstructionTemplateService instructionTemplateService)
        {
            _instructionTemplateService = instructionTemplateService;
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetPaginatedInstructionTemplates")]
        public async Task<ApiResponse> GetPaginatedInstructionTemplatesAsync([FromBody] PaginationSearchModel paginationSearchModel)
        {
            return await _instructionTemplateService.GetPaginatedInstructionTemplatesAsync(paginationSearchModel);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetInstructionTemplate")]
        public async Task<ApiResponse> GetInstructionTemplateAsync(long id)
        {
            return await _instructionTemplateService.GetInstructionTemplateAsync(id);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("SaveInstructionTemplate")]
        public async Task<ApiResponse> SaveInstructionTemplateAsync([FromBody] QuestionInstructionTemplateDto instructionTemplateDto)
        {
            return await _instructionTemplateService.SaveInstructionTemplateAsync(instructionTemplateDto);
        }

        [OESFilter(Authorize = true)]
        [HttpDelete("DeleteInstructionTemplate")]
        public async Task<ApiResponse> DeleteInstructionTemplateAsync(long id)
        {
            return await _instructionTemplateService.DeleteInstructionTemplateAsync(id);
        }
    }
}