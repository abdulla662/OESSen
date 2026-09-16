using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IEvaluationSyncService
    {
        Task<ApiResponse> SyncCandidateAnswersToEvaluationAsync();
    }
}
