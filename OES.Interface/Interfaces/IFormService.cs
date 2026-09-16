using OES.Helper.Dtos.Form;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IFormService
    {
        // GET METHODS:
        Task<ApiResponse> GetAllFormsWithTheirQuestionsByPaperIdAsync(long paperId);
        Task<ApiResponse> GetPaginatedFormsByPaperIdAsync(PaginationSearchModel paginationSearchModel, long paperId);
        Task<ApiResponse> GetFormByPaperId(long paperId);
        Task<ApiResponse> GetAllQuestionByFormIdAsync(long formId);
        Task<ApiResponse> GetFormWithQuestionsByFormIdAsync(long formId);
        Task<ApiResponse> GetFormSectionsWithQuestionsAsync(long formId);
        Task<ApiResponse> GetAllFormsByPaperIdAsync(long paperId);
        Task<ApiResponse> GetFormsWithEquationsByPaperIdsAsync(List<long> paperIds);
        Task<ApiResponse> GetFormsWithoutEquationsByPaperIdAsync(long paperId);
        Task<ApiResponse> GetQuestionWithFormsAsync(long paperId, long questionId);

        // POST/PUT/DELETE METHODS:
        Task<ApiResponse> CreateFormsForAutoPaperAsync(long paperId, int statingCounter, long paperFinalOutputFormsCount);
        Task<ApiResponse> CreateFormsForManualPaperAsync(long paperId, FormMetadataDto formMetadataDto);
        Task<ApiResponse> EditFormAsync(EditFormDto formDto, CancellationToken cancellationToken = default);
        Task<ApiResponse> AssignQuestionsToFormsAsync(List<FormQuestionsDto> formQuestions);
        Task<ApiResponse> SuspendFormAsync(long formId);
        Task<ApiResponse> ResetPaperFormsCounterAsync(long paperId);
        Task<ApiResponse> SoftDeleteFormAsync(long formId);
    }
}