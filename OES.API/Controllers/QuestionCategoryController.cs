using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class QuestionCategoryController(IQuestionCategoryService _questionCategoryService) : OESBaseController
    {

        [HttpPost("GetAllCategory")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetAllCategory(PaginationSearchModel searchModel)
        {
            return await _questionCategoryService.GetAllCategory(searchModel);
        }

        [HttpGet("GetCategories")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetCategories()
        {
            return await _questionCategoryService.GetCategories();
        }

        [HttpGet("GetCategoryById")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetCategoryById(long id)
        {
            return await _questionCategoryService.GetCategoryById(id);
        }

        [HttpPost("AddCategory")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddCategory(AddQuestionCategoryDto _addQuestion)
        {
            return await _questionCategoryService.AddCategory(_addQuestion);
        }

        [HttpPut("UpdateCategory")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UpdateCategory(QuestionCategoryDto _updateQuestion)
        {
            return await _questionCategoryService.UpdateCategory(_updateQuestion);
        }

        [HttpDelete("DeleteCategory")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> DeleteCategory(long id)
        {
            return await _questionCategoryService.SoftDeleteCategory(id);
        }

        [HttpPut("BulkUpdateCategoryFinalScores")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> BulkUpdateCategoryFinalScoresAsync(List<QuestionCategoryDto> _updatedCategories)
        {
            return await _questionCategoryService.BulkUpdateCategoryFinalScoresAsync(_updatedCategories);
        }

        [HttpGet("GetCategoriesByPaperId")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetCategoriesByPaperId(long id)
        {
            return await _questionCategoryService.GetCategoriesByPaperId(id);
        }

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetUserQuestionCategoryGroupsAsync")]
        //public async Task<IApiResponse> GetUserQuestionCategoryGroupsAsync()
        //{ 
        //    return await _questionCategoryService.GetUserQuestionCategoryGroupsAsync();
        //}

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetQuestionCategoryGroupsAsync")]
        //public async Task<IApiResponse> GetQuestionCategoryGroupsAsync(long questionCategoryId)
        //{ 
        //    return await _questionCategoryService.GetQuestionCategoryGroupsAsync(questionCategoryId);
        //}
    }
}
