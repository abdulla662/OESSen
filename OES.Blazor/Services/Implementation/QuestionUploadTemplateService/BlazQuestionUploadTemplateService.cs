using Newtonsoft.Json;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.QuestionUploadTemplate;
using OES.Helper.Dtos.QuestionUploadTemplateDto.OES.Helper.Dtos.UploadFiles;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.QuestionUploadTemplateService
{
    public class BlazQuestionUploadTemplateService : IBlazQuestionUploadTemplateService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazQuestionUploadTemplateService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<CustomTableData<QuestionUploadTemplateDto>> GetAllPaginatedTemplatesAsync(PaginationSearchModel paginationSearchModel)
        {
            var apiResponse = await _httpClientHelper.PostAsync(paginationSearchModel, "api/QuestionUploadTemplate/getAllPaginated");

            if (apiResponse?.Data != null)
            {
                return JsonConvert.DeserializeObject<CustomTableData<QuestionUploadTemplateDto>>(apiResponse.Data.ToString());
            }

            return new CustomTableData<QuestionUploadTemplateDto>([], 0);
        }

        public async Task<QuestionUploadTemplateDto> GetTemplateByIdAsync(long templateId)
        {
            var apiResponse = await _httpClientHelper.GetAsync<QuestionUploadTemplateDto>($"api/QuestionUploadTemplate/getById?{nameof(templateId)}={templateId}");

            return apiResponse?.Data as QuestionUploadTemplateDto ?? new();
        }

        public async Task<ApiResponse> AddTemplateAsync(QuestionUploadTemplateDto questionUploadTemplateDto)
        {
            return await _httpClientHelper.PostAsync(questionUploadTemplateDto, "api/QuestionUploadTemplate/add");
        }

        public async Task<ApiResponse> DeleteTemplateAsync(long templateId)
        {
            return await _httpClientHelper.DeleteAsync($"api/QuestionUploadTemplate/delete?{nameof(templateId)}={templateId}");
        }
    }
}
