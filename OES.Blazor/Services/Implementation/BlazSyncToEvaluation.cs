using OES.Blazor.Services.Interfaces;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation
{
    public class BlazSyncToEvaluation : IBlazSyncToEvaluation
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazSyncToEvaluation(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        #region ManualSync

        public async Task<ApiResponse> ManualSyncCandidateAnswersAsync()
        {
            return await _httpClientHelper.PostAsync(null, "api/EvaluationSync/syncCandidateAnswers");
        }

        #endregion ManualSync
    }
}
