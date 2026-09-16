using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.QuestionCataegory
{
    public interface IBlazQuestionCategoryService
    {
        Task<CustomTableData<QuestionCategoryDto>> GetAllCategories(PaginationSearchModel pagination);
        Task<List<QuestionCategoryDto>> GetCategories();
        Task<QuestionCategoryDto> GetCategoryById(long id);
        Task<ApiResponse> AddCategory(AddQuestionCategoryDto _addCategory);
        Task<ApiResponse> UpdateCategory(QuestionCategoryDto _updateCategory);
        Task<ApiResponse> SoftDeleteCategory(long id);
        Task<ApiResponse> BulkUpdateCategoryFinalScoresAsync(List<QuestionCategoryDto> _updatedCategories);
        Task<List<QuestionCategoryDto>> GetCategoriesByPaperId(long id);
        //Task<List<GetOESGroupDto>> GetUserQuestionCategoryGroupsAsync();
        //Task<QuestionCategoryGroupDto> GetQuestionCategoryGroupsAsync(long questionCategoryId);
    }
}
