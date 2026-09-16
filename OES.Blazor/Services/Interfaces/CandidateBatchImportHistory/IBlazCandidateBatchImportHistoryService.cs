using OES.Helper.Dtos.ScheduleCandidate.Responses;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.CandidateBatchImportHistory
{
    public interface IBlazCandidateBatchImportHistoryService
    {
        Task<ApiResponse> GetAllPaginatedBatchesAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> ReverseBatchAsync(GetCandidateBatchImportHistoryPaginationDto getCandidateBatchImportHistoryPaginationDto);

        Task<byte[]?> ExportBatchCandidatesAsync(long batchId);
    }
}
