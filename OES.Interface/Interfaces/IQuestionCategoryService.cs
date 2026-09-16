using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IQuestionCategoryService
    {
        Task<IApiResponse> GetAllCategory(PaginationSearchModel pagination);
        Task<IApiResponse> GetCategories();
        Task<IApiResponse> GetCategoryById(long id);
        Task<IApiResponse> AddCategory(AddQuestionCategoryDto _addCategory);
        Task<IApiResponse> UpdateCategory(QuestionCategoryDto _updateCategory);
        Task<IApiResponse> SoftDeleteCategory(long id);
        Task<IApiResponse> BulkUpdateCategoryFinalScoresAsync(List<QuestionCategoryDto> categoriesWithCustomNames);
        Task<IApiResponse> GetCategoriesByPaperId(long id);
        //Task<IApiResponse> GetUserQuestionCategoryGroupsAsync();
        //Task<IApiResponse> GetQuestionCategoryGroupsAsync(long questionCategoryId);
    }
}
