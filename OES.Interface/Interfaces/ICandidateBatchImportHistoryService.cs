using Microsoft.AspNetCore.Mvc;
using OES.Helper.Dtos.CandidateBatchImportHistory.Requests;
using OES.Helper.Dtos.ScheduleCandidate.Responses;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface ICandidateBatchImportHistoryService
    {
        Task<ApiResponse> GetAllPaginatedBatchesAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetBatchByIdAsync(long id);

        Task<ApiResponse> AddBatchAsync(AddCandidateBatchImportHistoryRequestDto addCandidateBatchImportHistoryRequestDto);

        Task<ApiResponse> ReverseBatchAsync(GetCandidateBatchImportHistoryPaginationDto getCandidateBatchImportHistoryPaginationDto);

        Task<ApiResponse> DeleteBatchAsync(long id);

        Task<IActionResult> ExportBatchCandidatesAsync(long batchId);
    }
}
