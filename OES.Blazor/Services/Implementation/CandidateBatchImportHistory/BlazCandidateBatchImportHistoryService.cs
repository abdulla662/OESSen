using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.CandidateBatchImportHistory;
using OES.Helper.Dtos.ScheduleCandidate.Responses;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.CandidateBatchImportHistory
{
    public class BlazCandidateBatchImportHistoryService : IBlazCandidateBatchImportHistoryService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazCandidateBatchImportHistoryService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<ApiResponse> GetAllPaginatedBatchesAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _httpClientHelper.PostAsync(paginationSearchModel, "api/CandidateBatchImportHistory/GetAllPaginatedBatches");
        }

        public async Task<ApiResponse> ReverseBatchAsync(GetCandidateBatchImportHistoryPaginationDto getCandidateBatchImportHistoryPaginationDto)
        {
            return await _httpClientHelper.PostAsync(getCandidateBatchImportHistoryPaginationDto, "api/CandidateBatchImportHistory/ReverseBatch");
        }

        public async Task<byte[]?> ExportBatchCandidatesAsync(long batchId)
        {
            var response = await _httpClientHelper._httpClient.GetAsync($"api/CandidateBatchImportHistory/ExportBatchCandidates?{nameof(batchId)}={batchId}");
            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}

