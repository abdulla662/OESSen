using OES.Helper.Dtos.QyestionType;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IQuestionTypeService
    {
        Task<IApiResponse> GetAllQuestionType(PaginationSearchModel pagination);
        Task<ApiResponse> CreateQuestionType(QuestionTypeDto questionTypeDto);
        Task<ApiResponse> UpdateQuestionType(QuestionTypeDto questionTypeUpdateDto);
        Task<ApiResponse> DeleteQuestionType(long id);
        Task<ApiResponse> GetQuestionTypeById(long id);
        Task<ApiResponse> GetAllQuestionTypes();
    }
}
