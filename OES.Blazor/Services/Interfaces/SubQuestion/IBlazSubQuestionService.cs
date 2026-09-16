using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.SubQuestion
{
    public interface IBlazSubQuestionService
    {
        Task<CustomTableData<PaginatedListSubQuestionDto>> PaginationSubQuestions(long parentId, PaginationSearchModel pagination);
        Task<List<LanguageDto>> GetAllQuestionLanguageList(long Id);
    }
}
