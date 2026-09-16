using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Question
{
    public class BlazQuestionInstructionTemplateService(
        IHttpClientHelper _httpClientHelper,
        IBlazGetCustomTableData<QuestionInstructionTemplateDto> _blazGetCustomTableData
    ) : IBlazQuestionInstructionTemplate
    {
        public async Task<CustomTableData<QuestionInstructionTemplateDto>> GetPaginatedInstructionTemplatesAsync(PaginationSearchModel paginationSearchModel)
        {
            var response = await _blazGetCustomTableData.GetCustomTableData(paginationSearchModel, "api/QuestionInstructionTemplate/GetPaginatedInstructionTemplates");

            return response;
        }

        public async Task<ApiResponse> GetInstructionTemplateAsync(long id)
        {
            return await _httpClientHelper.GetAsync<QuestionInstructionTemplateDto>($"api/QuestionInstructionTemplate/GetInstructionTemplate?{nameof(id)}={id}");
        }

        public async Task<ApiResponse> SaveInstructionTemplateAsync(QuestionInstructionTemplateDto instructionTemplateDto)
        {
            return await _httpClientHelper.PostAsync(instructionTemplateDto, "api/QuestionInstructionTemplate/SaveInstructionTemplate");
        }

        public async Task<ApiResponse> DeleteInstructionTemplateAsync(long id)
        {
            return await _httpClientHelper.DeleteAsync($"api/QuestionInstructionTemplate/DeleteInstructionTemplate?{nameof(id)}={id}");
        }
    }
}
