using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Form;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class FormController : OESBaseController
    {
        private readonly IFormService _formService;

        public FormController(IFormService formService)
        {
            _formService = formService;
        }

        // GET METHODS:

        [OESFilter(Authorize = true)]
        [HttpGet("GetAllFormsWithTheirQuestionsByPaperId")]
        public async Task<ApiResponse> GetAllFormsWithTheirQuestionsByPaperIdAsync(long paperId)
        {
            return await _formService.GetAllFormsWithTheirQuestionsByPaperIdAsync(paperId);
        }

        [HttpPost("GetPaginatedFormsByPaperId")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetPaginatedFormsByPaperIdAsync(PaginationSearchModel paginationSearchModel, long paperId)
        {
            return await _formService.GetPaginatedFormsByPaperIdAsync(paginationSearchModel, paperId);
        }

        [HttpGet("GetFormByPaperId")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetFormByPaperId(long paperId)
        {
            return await _formService.GetFormByPaperId(paperId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetAllQuestionByFormId")]
        public async Task<ApiResponse> GetAllQuestionByFormIdAsync(long formId)
        {
            return await _formService.GetAllQuestionByFormIdAsync(formId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetFormWithQuestionsByFormId")]
        public async Task<ApiResponse> GetFormWithQuestionsByFormIdAsync(long formId)
        {
            return await _formService.GetFormWithQuestionsByFormIdAsync(formId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetFormSectionsWithQuestions")]
        public async Task<ApiResponse> GetFormSectionsWithQuestionsAsync(long formId)
        {
            return await _formService.GetFormSectionsWithQuestionsAsync(formId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetAllFormsByPaperIdAsync")]
        public async Task<ApiResponse> GetAllFormsByPaperIdAsync(long paperId)
        {
            return await _formService.GetAllFormsByPaperIdAsync(paperId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetQuestionWithForms")]
        public async Task<ApiResponse> GetQuestionWithFormsAsync(long paperId, long questionId)
        {
            return await _formService.GetQuestionWithFormsAsync(paperId, questionId);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetFormsWithEquationsByPaperIds")]
        public async Task<ApiResponse> GetFormsWithEquationsByPaperIdsAsync(List<long> paperIds)
        {
            return await _formService.GetFormsWithEquationsByPaperIdsAsync(paperIds);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetFormsWithoutEquationsByPaperId")]
        public async Task<ApiResponse> GetFormsWithoutEquationsByPaperIdAsync(long paperId)
        {
            return await _formService.GetFormsWithoutEquationsByPaperIdAsync(paperId);
        }

        // POST/PUT/DELETE METHODS:

        [HttpPost("EditForm")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> EditFormAsync(EditFormDto formDto, CancellationToken cancellationToken = default)
        {
            return await _formService.EditFormAsync(formDto, cancellationToken);
        }

        [HttpPost("SuspendForm")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> SuspendFormAsync([FromBody] long formId)
        {
            return await _formService.SuspendFormAsync(formId);
        }

        [HttpDelete("ResetPaperFormsCounter")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> ResetPaperFormsCounterAsync(long paperId)
        {
            return await _formService.ResetPaperFormsCounterAsync(paperId);
        }

        [HttpDelete("SoftDeleteForm")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> SoftDeleteFormAsync(long formId)
        {
            return await _formService.SoftDeleteFormAsync(formId);
        }
    }
}