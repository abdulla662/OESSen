using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.EquationTemplate;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.General;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.EquationTemplate
{
    public class BlazEquationTemplateService : IBlazEquationTemplateService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IBlazGetCustomTableData<EquationTemplatePaginationDto> _blazGetCustomTableData;

        public BlazEquationTemplateService(IHttpClientHelper httpClientHelper, IBlazGetCustomTableData<EquationTemplatePaginationDto> blazGetCustomTableData)
        {
            _httpClientHelper = httpClientHelper;
            _blazGetCustomTableData = blazGetCustomTableData;
        }

        public async Task<CustomTableData<EquationTemplatePaginationDto>> GetAllEquationTemplatesAsync(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTableData.GetCustomTableData(pagination, "api/EquationTemplate/GetAllEquationTemplatesAsync");
        }

        public async Task<ApiResponse> AddEquationTemplateAsync(AddOrUpdateEquationTemplateDto request)
        {
            var response = await _httpClientHelper.PostAsync(request, "api/EquationTemplate/AddEquationTemplateAsync");
            return response;
        }

        public Task<ApiResponse> GetEquationTemplateById(long id)
        {
            return _httpClientHelper.GetAsync<GetEquationTemplateResponseDto>($"api/EquationTemplate/GetEquationTemplateById?{nameof(id)}={id}");
        }

        public Task<ApiResponse> UpdateEquationTemplateAsync(AddOrUpdateEquationTemplateDto request)
        {
            return _httpClientHelper.PostAsync(request, "api/EquationTemplate/UpdateEquationTemplateAsync");
        }

        public Task<ApiResponse> DeleteEquationTemplateAsync(long id)
        {
            return _httpClientHelper.DeleteAsync($"api/EquationTemplate/DeleteEquationTemplateAsync?{nameof(id)}={id}");
        }

        public Task<ApiResponse> GetCandidateQuestionsWithEquation(long formId)
        {
            return _httpClientHelper.GetAsync<CandidateEquationResultDto>($"api/EquationTemplate/GetCandidateQuestionsWithEquation?id={formId}");
        }

        public Task<ApiResponse> ExportCandidatesData(ExportCandidatesRequestDto request)
        {
            return _httpClientHelper.PostAsync(request, "api/EquationTemplate/ExportCandidatesData");
        }

        public Task<ApiResponse> GenerateResultsBatchAsync(ExportCandidateRequestByDateDto request)
        {
            return _httpClientHelper.PostAsync(request, "api/EquationTemplate/GenerateResultsBatch");
        }

        public Task<ApiResponse> ExportCandidatesDataBatchAsync(ExportCandidatesBatchRequestDto request)
        {
            return _httpClientHelper.PostAsync(request, "api/EquationTemplate/ExportCandidatesDataBatch");
        }

        public async Task<List<ItemBanksFromItemBankPointResponseDto>> GetAllItemBanksFromQuestionBlocksAsync(long paperId)
        {
            var response = await _httpClientHelper.GetAsync<List<ItemBanksFromItemBankPointResponseDto>>($"api/EquationTemplate/GetAllItemBanksFromQuestionBlocks?id={paperId}");

            return (List<ItemBanksFromItemBankPointResponseDto>)response.Data;
        }

        public async Task<List<CandidateResultDto>> GetCandidateResultsAsync(ExportCandidatesRequestDto request)
        {
            var apiResponse = await _httpClientHelper.PostAsync(request, "api/EquationTemplate/GetCandidateResults");

            if (apiResponse.Data == null)
            {
                return [];
            }

            var data = (JsonElement)apiResponse.Data;

            var result = JsonSerializer.Deserialize<List<CandidateResultDto>>(
                data.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return result ?? [];
        }

        //public async Task<List<GetOESGroupDto>> GetUserEquationGroupsAsync()
        //{
        //    var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/EquationTemplate/GetUserEquationGroupsAsync");

        //    var data = (List<GetOESGroupDto>)response.Data;

        //    return data ?? [];
        //}

        //public async Task<EquationGroupDto> GetEquationGroupsAsync(long EquationId)
        //{
        //    var response = await _httpClientHelper.GetAsync<EquationGroupDto>($"api/EquationTemplate/GetEquationGroupsAsync?EquationId={EquationId}");

        //    return response.Data as EquationGroupDto ?? new EquationGroupDto();
        //}
    }
}