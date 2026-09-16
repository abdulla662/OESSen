using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Question
{
    public interface IBlazFormQuestionScoreService
    {
        Task<ApiResponse> GetQuestionForPaperAsync(long paperId);
    }
}
