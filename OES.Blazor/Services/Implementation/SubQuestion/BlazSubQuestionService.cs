using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.SubQuestion;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.SubQuestion
{
    public class BlazSubQuestionService(IBlazGetCustomTableData<PaginatedListSubQuestionDto> _blazGetCustomTableData, IHttpClientHelper _httpClientHelper) : IBlazSubQuestionService
    {
        public async Task<CustomTableData<PaginatedListSubQuestionDto>> PaginationSubQuestions(long parentId, PaginationSearchModel pagination)
        {
            string apiPath = $"api/QuestionMetadata/PaginatedSubQuestions?parentId={parentId}";

            var response = await _blazGetCustomTableData.GetCustomTableData(pagination, apiPath);

            return response;
        }

        public async Task<List<LanguageDto>> GetAllQuestionLanguageList(long Id)
        {
            var response = await _httpClientHelper.GetAsync<List<LanguageDto>>($"api/Language/GetAllQuestionLanguageList?ID={Id}");

            return (List<LanguageDto>)response.Data;
        }
    }
}
