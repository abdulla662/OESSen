using OES.Helper.Dtos.Form;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IFormQuestionScoreService
    {
        Task<ApiResponse> GetQuestionsRelationsForManualPaperAsync(long paperId);

        Task<ApiResponse> SaveFormsAndItsQuestions(long paperId, Dictionary<string, List<long>> formQuestionMap, FormMetadataDto formMetadata);

        Task<ApiResponse> ApplyScoresToPaperFormsAsync(long paperId);
    }
}