using OES.Helper.Dtos.Form;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.Section;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Form
{
    public interface IBlazFormService
    {
        // GET METHODS:

        Task<GetPaperWithFormsDto> GetAllFormsWithTheirQuestionsByPaperIdAsync(long paperId);

        Task<CustomTableData<FormListDto>> GetPaginatedFormsByPaperIdAsync(PaginationSearchModel paginationSearchModel, long paperId);

        Task<List<FormListDto>> GetFormByPaperId(long paperId);

        Task<FormQuestionsDto> GetFormWithQuestionsByFormIdAsync(long formId);

        Task<List<FormQuestionDetailedDto>> GetAllQuestionByFormIdAsync(long formId);

        Task<List<SectionWithFormQuestionsDto>> GetFormSectionsWithQuestionsAsync(long formId);

        Task<List<GetFormDto>> GetAllFormsByPaperIdAsync(long paperId);

        Task<List<GetFormDto>> GetFormsWithEquationsByPaperIdsAsync(List<long> paperIds);

        Task<List<GetFormDto>> GetFormsWithoutEquationsByPaperIdAsync(long paperId);

        Task<QuestionWithFormsDto> GetQuestionWithFormsAsync(long paperId, long questionId);


        // POST/PUT/DELETE METHODS:

        Task<ApiResponse> EditFormAsync(EditFormDto formDto);

        Task<ApiResponse> ResetPaperFormsCounterAsync(long paperId);

        Task<ApiResponse> SoftDeleteFormAsync(long formId);
    }
}