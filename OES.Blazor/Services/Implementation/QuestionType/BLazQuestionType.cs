using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Blazor.Services.Implementation.QuestionType
{
    public class BLazQuestionType(IHttpClientHelper _httpClientHelper, IBlazGetCustomTableData<QuestionTypeDto> _blazGetCustomTableData) : IBLazQuestionType
    {
        public async Task<CustomTableData<QuestionTypeDto>> PaginationQuestionType(PaginationSearchModel pagination)
        {
            var response = await _blazGetCustomTableData.GetCustomTableData(pagination, "api/QuestionType/PaginatedQuestionType");

            return response;
        }

        public async Task<IApiResponse> CreateQuestionType(QuestionTypeDto questionType)
        {
            var response = await _httpClientHelper.PostAsync(questionType, "api/QuestionType/CreateQuestionType");

            return response;
        }

        public async Task<IApiResponse> UpdateQuestionType(QuestionTypeDto questionType)
        {
            return await _httpClientHelper.PostAsync(questionType, $"api/QuestionType/UpdateQuestionType");
        }

        public async Task<IApiResponse> DeleteQuestionType(long id)
        {
            return await _httpClientHelper.DeleteAsync($"api/QuestionType/DeleteQuestionType?id={id}");
        }

        public async Task<ApiResponse> GetQuestionType(long Id)
        {
            return await _httpClientHelper.GetAsync<QuestionTypeDto>($"api/QuestionType/GetQuestionTypeById?id={Id}");
        }

        public async Task<List<QuestionTypeDto>> GetAllQuestionType()
        {
            var response = await _httpClientHelper.GetAsync<List<QuestionTypeDto>>("api/QuestionType/GetAllQuestionTypes");

            var questionTypes = (List<QuestionTypeDto>)response.Data ?? [];

            return questionTypes;
        }

        public async Task<List<DifficultyLevelDto>> GetAllDifficultyLevels(long profileId)
        {
            var response = await _httpClientHelper.GetAsync<List<DifficultyLevelDto>>($"api/DifficultyLevel/GetDifficultyLevelByProfileId?profileId={profileId}");

            return (List<DifficultyLevelDto>)response.Data ?? [];
        }
    }
}
