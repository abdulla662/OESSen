using OES.Helper.Enums;
using OES.Helper.General;
using SharedHelper.Enums;

namespace OES.Interface.Interfaces
{
    public interface ISectionSummaryService
    {
        Task<ApiResponse> GetSectionSummaryAsync(long paperId, QuestionSelectionType questionSelectionType, PaperType paperType);
    }
}
