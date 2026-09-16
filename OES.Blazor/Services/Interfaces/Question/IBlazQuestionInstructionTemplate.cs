using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Question
{
    public interface IBlazQuestionInstructionTemplate
    {
        Task<CustomTableData<QuestionInstructionTemplateDto>> GetPaginatedInstructionTemplatesAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetInstructionTemplateAsync(long id);

        Task<ApiResponse> SaveInstructionTemplateAsync(QuestionInstructionTemplateDto instructionTemplateDto);

        Task<ApiResponse> DeleteInstructionTemplateAsync(long id);
    }
}
