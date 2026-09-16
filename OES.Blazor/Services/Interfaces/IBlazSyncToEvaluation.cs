using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces
{
    public interface IBlazSyncToEvaluation
    {
        Task<ApiResponse> ManualSyncCandidateAnswersAsync();
    }
}
