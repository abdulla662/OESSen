using OES.Helper.Enums;
using OES.Helper.General;
using SharedHelper.Enums;

namespace OES.Blazor.Services.Interfaces.Paper
{
    public interface IBlazPaperSummaryService
    {
        Task<ApiResponse> GetAdaptiveSectionSummary(long paperId, QuestionSelectionType questionSelectionType, PaperType paperType);

        Task<ApiResponse> GetStandardSectionSummaryAsync<TDto>(long paperId, QuestionSelectionType questionSelectionType, PaperType paperType);
    }
}