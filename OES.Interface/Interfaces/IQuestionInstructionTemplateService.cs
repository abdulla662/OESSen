using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IQuestionInstructionTemplateService
    {
        Task<ApiResponse> GetPaginatedInstructionTemplatesAsync(PaginationSearchModel paginationSearchModel);
        Task<ApiResponse> GetInstructionTemplateAsync(long id);
        Task<ApiResponse> SaveInstructionTemplateAsync(QuestionInstructionTemplateDto instructionTemplateDto);
        Task<ApiResponse> DeleteInstructionTemplateAsync(long id);
    }
}
