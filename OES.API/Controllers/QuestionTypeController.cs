using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class QuestionTypeController(IQuestionTypeService _questionTypeService, IPaginationSearchModel _paginationSearchModel) : OESBaseController
    {
        /// <summary>
        /// Retrieves a paginated list of Question Types based on the provided search criteria.
        /// </summary>
        /// <param name="paginationSearchModel">Pagination and search parameters.</param>
        /// <returns>A paginated list of Question Types wrapped in an ApiResponse.</returns>
        [HttpPost("PaginatedQuestionType")]
        [OESFilter(Authorize = true, ApplySignatureFilter = false, ApplyOrganizationIdFilter = false)]
        public async Task<ApiResponse> PaginatedQuestionTypes(PaginationSearchModel paginationSearchModel)
        {
            return (ApiResponse)await _questionTypeService.GetAllQuestionType(paginationSearchModel);
        }

        /// <summary>
        /// Creates a new Question Type.
        /// </summary>
        /// <param name="dto">The data transfer object (DTO) representing the Question Type to create.</param>
        /// <returns>An IApiResponse indicating the success or failure of the creation operation.</returns>
        [HttpPost("CreateQuestionType")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> CreateQuestionType(QuestionTypeDto dto)
        {
            return await _questionTypeService.CreateQuestionType(dto);
        }

        /// <summary>
        /// Updates an existing Question Type.
        /// </summary>
        /// <param name="dto">The data transfer object (DTO) representing the updated Question Type.</param>
        /// <returns>An IApiResponse indicating the success or failure of the update operation.</returns>
        [HttpPost("UpdateQuestionType")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> EditQuestionType(QuestionTypeDto dto)
        {
            return await _questionTypeService.UpdateQuestionType(dto);
        }

        /// <summary>
        /// Deletes a Question Type by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Question Type to delete.</param>
        /// <returns>An IApiResponse indicating the success or failure of the deletion operation.</returns>
        [HttpDelete("DeleteQuestionType")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> DeleteQuestionType(long id)
        {
            return await _questionTypeService.DeleteQuestionType(id);
        }

        /// <summary>
        /// Retrieves the details of a specific Question Type by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Question Type to retrieve.</param>
        /// <returns>An IApiResponse containing the details of the specified Question Type.</returns>
        [HttpGet("GetQuestionTypeById")]
        [OESFilter(Authorize = true, ApplySignatureFilter = false, ApplyOrganizationIdFilter = false)]
        public async Task<IApiResponse> GetQuestionTypeById(long id)
        {
            return await _questionTypeService.GetQuestionTypeById(id);
        }

        /// <summary>
        /// Retrieves a list of all Question Types.
        /// </summary>
        /// <returns>An ApiResponse containing the list of all Question Types.</returns>
        [HttpGet("GetAllQuestionTypes")]
        [OESFilter(Authorize = true, ApplySignatureFilter = false, ApplyOrganizationIdFilter = false)]
        public async Task<IApiResponse> GetAllQuestionTypes()
        {
            return await _questionTypeService.GetAllQuestionTypes();
        }
    }
}
