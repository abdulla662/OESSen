using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class EquationTemplateController : OESBaseController
    {
        private readonly IEquationTemplateService _equationTemplateService;


        public EquationTemplateController(IEquationTemplateService equationTemplateService)
        {
            _equationTemplateService = equationTemplateService;
        }


        [HttpPost("AddEquationTemplateAsync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddEquationTemplateAsync(AddOrUpdateEquationTemplateDto addOrUpdateEquationTemplateDto, CancellationToken cancellationToken)
        {
            return await _equationTemplateService.AddEquationTemplateAsync(addOrUpdateEquationTemplateDto, cancellationToken);
        }


        [HttpPost("GetAllEquationTemplatesAsync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetPaginatedSubjects([FromBody] PaginationSearchModel pagination)
        {
            return await _equationTemplateService.GetAllEquationTemplatesAsync(pagination);
        }


        [HttpGet("GetEquationTemplateById")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetEquationTemplateById(long id)
        {
            return await _equationTemplateService.GetEquationTemplateById(id);
        }


        //[OESFilter(Authorize = true)]
        //[HttpGet("GetUserEquationGroupsAsync")]
        //public async Task<ApiResponse> GetUserEquationGroupsAsync()
        //{
        //    return await _equationTemplateService.GetUserEquationGroupsAsync();
        //}


        //[OESFilter(Authorize = true)]
        //[HttpGet("GetEquationGroupsAsync")]
        //public async Task<IApiResponse> GetEquationGroupsAsync(long blockId)
        //{
        //    return await _equationTemplateService.GetEquationGroupsAsync(blockId);
        //}


        [HttpPost("UpdateEquationTemplateAsync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateEquationTemplateAsync(AddOrUpdateEquationTemplateDto addOrUpdateEquationTemplateDto)
        {
            return await _equationTemplateService.UpdateEquationTemplateAsync(addOrUpdateEquationTemplateDto);
        }


        [HttpDelete("DeleteEquationTemplateAsync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DeleteEquationTemplateAsync(long id)
        {
            return await _equationTemplateService.DeleteEquationTemplateAsync(id);
        }


        [HttpGet("GetCandidateQuestionsWithEquation")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetCandidateQuestionsWithEquation(long id)
        {
            return await _equationTemplateService.GetCandidateQuestionsWithEquation(id);
        }


        [HttpPost("ExportCandidatesData")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> ExportCandidatesData(ExportCandidatesRequestDto request)
        {
            return await _equationTemplateService.ExportCandidatesData(request);
        }


        [HttpGet("GetAllItemBanksFromQuestionBlocks")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllItemBanksFromQuestionBlocksAsync(long id)
        {
            return await _equationTemplateService.GetAllItemBanksFromQuestionBlocksAsync(id);
        }


        [HttpPost("GetCandidateResults")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetCandidateResults(ExportCandidatesRequestDto request)
        {
            return await _equationTemplateService.GetCandidateResultsAsync(request);
        }


        [HttpPost("GenerateResultsBatch")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GenerateResultsBatch(
            ExportCandidateRequestByDateDto request,
            CancellationToken cancellationToken)
        {
            return await _equationTemplateService.GenerateResultsBatchAsync(request, cancellationToken);
        }


        [HttpPost("ExportCandidatesDataBatch")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> ExportCandidatesDataBatch(
            ExportCandidatesBatchRequestDto request,
            CancellationToken cancellationToken)
        {
            return await _equationTemplateService.ExportCandidatesDataBatchAsync(request, cancellationToken);
        }
    }
}