using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.QuestionCategory
{
    public class BlazQuestionCategoryService : IBlazQuestionCategoryService
    {
        private readonly IBlazGetCustomTableData<QuestionCategoryDto> _blazGetCustomTableData;
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazQuestionCategoryService(IBlazGetCustomTableData<QuestionCategoryDto> blazGetCustomTableData,
                                           IHttpClientHelper httpClientHelper)
        {
            _blazGetCustomTableData = blazGetCustomTableData;
            _httpClientHelper = httpClientHelper;
        }


        public async Task<CustomTableData<QuestionCategoryDto>> GetAllCategories(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTableData.GetCustomTableData(pagination, "api/QuestionCategory/GetAllCategory");
        }

        public async Task<List<QuestionCategoryDto>> GetCategories()
        {
            var result = await _httpClientHelper.GetAsync<List<QuestionCategoryDto>>("api/QuestionCategory/GetCategories");

            return (List<QuestionCategoryDto>)result.Data;
        }

        public async Task<QuestionCategoryDto> GetCategoryById(long id)
        {
            var result = await _httpClientHelper.GetAsync<QuestionCategoryDto>($"api/QuestionCategory/GetCategoryById?id={id}");

            return (QuestionCategoryDto)result.Data;
        }

        public async Task<ApiResponse> AddCategory(AddQuestionCategoryDto _addCategory)
        {
            return await _httpClientHelper.PostAsync(_addCategory, "api/QuestionCategory/AddCategory");
        }

        public async Task<ApiResponse> UpdateCategory(QuestionCategoryDto _updateCategory)
        {
            return await _httpClientHelper.PutAsync(_updateCategory, "api/QuestionCategory/UpdateCategory");
        }

        public async Task<ApiResponse> SoftDeleteCategory(long id)
        {
            var response = await _httpClientHelper.DeleteAsync($"api/QuestionCategory/DeleteCategory?{nameof(id)}={id}");

            return response;
        }

        public async Task<ApiResponse> BulkUpdateCategoryFinalScoresAsync(List<QuestionCategoryDto> _updatedCategories)
        {
            return await _httpClientHelper.PutAsync(_updatedCategories, "api/QuestionCategory/BulkUpdateCategoryFinalScores");
        }

        public async Task<List<QuestionCategoryDto>> GetCategoriesByPaperId(long id)
        {
            var result = await _httpClientHelper.GetAsync<List<QuestionCategoryDto>>($"api/QuestionCategory/GetCategoriesByPaperId?{nameof(id)}={id}");

            return (List<QuestionCategoryDto>)result.Data ?? [];
        }

        //public async Task<List<GetOESGroupDto>> GetUserQuestionCategoryGroupsAsync()
        //{
        //    var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/QuestionCategory/GetUserQuestionCategoryGroupsAsync");
        //    return (List<GetOESGroupDto>)response.Data ?? [];
        //}

        //public async Task<QuestionCategoryGroupDto> GetQuestionCategoryGroupsAsync(long questionCategoryId)
        //{
        //    var response = await _httpClientHelper.GetAsync<QuestionCategoryGroupDto>($"api/QuestionCategory/GetQuestionCategoryGroupsAsync?questionCategoryId={questionCategoryId}");
        //    return response.Data as QuestionCategoryGroupDto ?? new QuestionCategoryGroupDto();
        //}
    }
}
