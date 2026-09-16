using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.ScheduleCandidate.Responses;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class CandidateBatchImportHistoryController : OESBaseController
    {
        private readonly ICandidateBatchImportHistoryService _candidateBatchImportHistoryService;

        public CandidateBatchImportHistoryController(ICandidateBatchImportHistoryService candidateBatchImportHistoryService)
        {
            _candidateBatchImportHistoryService = candidateBatchImportHistoryService;
        }

        [HttpPost("GetAllPaginatedBatches")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllPaginatedBatchesAsync(PaginationSearchModel pagination)
            => await _candidateBatchImportHistoryService.GetAllPaginatedBatchesAsync(pagination);

        [HttpPost("ReverseBatch")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> ReverseBatchAsync(GetCandidateBatchImportHistoryPaginationDto getCandidateBatchImportHistoryPaginationDto)
            => await _candidateBatchImportHistoryService.ReverseBatchAsync(getCandidateBatchImportHistoryPaginationDto);

        [HttpGet("ExportBatchCandidates")]
        [OESFilter(Authorize = true)]
        public async Task<IActionResult> ExportBatchCandidatesAsync(long batchId)
            => await _candidateBatchImportHistoryService.ExportBatchCandidatesAsync(batchId);
    }
}

