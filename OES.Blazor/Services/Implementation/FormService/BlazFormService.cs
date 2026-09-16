using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Form;
using OES.Helper.Dtos.Form;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.Section;
using OES.Helper.General;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.FormService
{
    public class BlazFormService : IBlazFormService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        private readonly IBlazGetCustomTableData<FormListDto> _blazGetCustomTableData;


        public BlazFormService(IHttpClientHelper httpClientHelper, IBlazGetCustomTableData<FormListDto> blazGetCustomTableData)
        {
            _httpClientHelper = httpClientHelper;
            _blazGetCustomTableData = blazGetCustomTableData;
        }


        // GET METHODS:

        public async Task<GetPaperWithFormsDto> GetAllFormsWithTheirQuestionsByPaperIdAsync(long paperId)
        {
            var response = await _httpClientHelper.GetAsync<GetPaperWithFormsDto>($"api/Form/GetAllFormsWithTheirQuestionsByPaperId?{nameof(paperId)}={paperId}");

            return (GetPaperWithFormsDto)response.Data ?? new();
        }

        public async Task<CustomTableData<FormListDto>> GetPaginatedFormsByPaperIdAsync(PaginationSearchModel paginationSearchModel, long paperId)
        {
            var response = await _blazGetCustomTableData.GetCustomTableData(paginationSearchModel, $"api/Form/GetPaginatedFormsByPaperId?{nameof(paperId)}={paperId}");

            return response;
        }

        public async Task<List<FormListDto>> GetFormByPaperId(long paperId)
        {
            var response = await _httpClientHelper.GetAsync<List<FormListDto>>($"api/Form/GetFormByPaperId?{nameof(paperId)}={paperId}");

            return (List<FormListDto>)response.Data;
        }

        public async Task<FormQuestionsDto> GetFormWithQuestionsByFormIdAsync(long formId)
        {
            var response = await _httpClientHelper.GetAsync<FormQuestionsDto>($"api/Form/GetFormWithQuestionsByFormId?{nameof(formId)}={formId}");

            return (FormQuestionsDto)response.Data;
        }

        public async Task<List<FormQuestionDetailedDto>> GetAllQuestionByFormIdAsync(long formId)
        {
            var response = await _httpClientHelper.GetAsync<List<FormQuestionDetailedDto>>($"api/Form/GetAllQuestionByFormId?{nameof(formId)}={formId}");

            return (List<FormQuestionDetailedDto>)response.Data;
        }

        public async Task<List<SectionWithFormQuestionsDto>> GetFormSectionsWithQuestionsAsync(long formId)
        {
            var response = await _httpClientHelper.GetAsync<List<SectionWithFormQuestionsDto>>($"api/Form/GetFormSectionsWithQuestions?{nameof(formId)}={formId}");

            return (List<SectionWithFormQuestionsDto>)response.Data ?? [];
        }

        public async Task<List<GetFormDto>> GetAllFormsByPaperIdAsync(long paperId)
        {
            var response = await _httpClientHelper.GetAsync<List<GetFormDto>>($"api/Form/GetAllFormsByPaperIdAsync?{nameof(paperId)}={paperId}");

            return (List<GetFormDto>)response.Data ?? null;
        }

        public async Task<List<GetFormDto>> GetFormsWithEquationsByPaperIdsAsync(List<long> paperIds)
        {
            var response = await _httpClientHelper.PostAsync(paperIds, "api/Form/GetFormsWithEquationsByPaperIds");

            return response.Data != null ? JsonSerializer.Deserialize<List<GetFormDto>>(response.Data.ToString()!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
        }

        public async Task<List<GetFormDto>> GetFormsWithoutEquationsByPaperIdAsync(long paperId)
        {
            var response = await _httpClientHelper.GetAsync<List<GetFormDto>>($"api/Form/GetFormsWithoutEquationsByPaperId?{nameof(paperId)}={paperId}");

            return (List<GetFormDto>)response.Data ?? [];
        }

        public async Task<QuestionWithFormsDto> GetQuestionWithFormsAsync(long paperId, long questionId)
        {
            var response = await _httpClientHelper.GetAsync<QuestionWithFormsDto>(
                $"api/Form/GetQuestionWithForms?{nameof(paperId)}={paperId}&{nameof(questionId)}={questionId}"
            );

            return (QuestionWithFormsDto)response.Data ?? new();
        }


        // POST/PUT/DELETE METHODS:

        public async Task<ApiResponse> EditFormAsync(EditFormDto formDto)
        {
            return await _httpClientHelper.PostAsync(formDto, "api/Form/EditForm");
        }

        public async Task<ApiResponse> ResetPaperFormsCounterAsync(long paperId)
        {
            return await _httpClientHelper.DeleteAsync($"api/Form/ResetPaperFormsCounter?{nameof(paperId)}={paperId}");
        }

        public async Task<ApiResponse> SoftDeleteFormAsync(long formId)
        {
            return await _httpClientHelper.DeleteAsync($"api/Form/SoftDeleteForm?{nameof(formId)}={formId}");
        }
    }
}