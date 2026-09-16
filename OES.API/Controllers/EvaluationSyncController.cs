using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class EvaluationSyncController : OESBaseController, IEvaluationSyncService
    {
        private readonly IEvaluationSyncService _evaluationSyncService;

        public EvaluationSyncController(IEvaluationSyncService evaluationSyncService)
        {
            _evaluationSyncService = evaluationSyncService;
        }

        [HttpPost("syncCandidateAnswers")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> SyncCandidateAnswersToEvaluationAsync()
        {
            return await _evaluationSyncService.SyncCandidateAnswersToEvaluationAsync();
        }
    }
}
