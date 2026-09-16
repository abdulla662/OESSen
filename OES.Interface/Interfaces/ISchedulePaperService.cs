using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface ISchedulePaperService
    {
        Task<ApiResponse> GetAllSchedulePapersByScheduleIdAsync(PaginationSearchModel paginationSearchModel, long scheduleId);

        Task<ApiResponse> GetSchedulePaperByIdAsync(long schedulePaperId);

        Task<ApiResponse> GetSchedulePaperSummaryAsync(long scheduleId);

        Task<ApiResponse> AddSchedulePaperAsync(AddOrUpdateSchedulePaperRequestDto addSchedulePaperRequestDto);

        Task<ApiResponse> UpdateSchedulePaperAsync(AddOrUpdateSchedulePaperRequestDto updateSchedulePaperRequestDto);

        Task<ApiResponse> DeleteSchedulePaperAsync(long schedulePaperId);

        Task<ApiResponse> GetSchedulePaperAllocation(long schedulePaperId);

        Task<ApiResponse> DeletePaperAllocationAsync(long venueId, long schedulePaperId);

        Task<ApiResponse> GetSchedulePapersAsync(long scheduleMetadataId);

        Task<ApiResponse> GetVenuesWithCandidatesCountByPaperIdAsync(PaginationSearchModel paginationSearchModel, long paperId);

        Task<ApiResponse> GetSchedulePaperVenuesAsync(long schedulePaperId);

        Task<byte[]> ExportSchedulePaperCandidatesToExcelAsync(long schedulePaperId);

        Task<ApiResponse> GetVenueBatchesAsync(long venueId, long paperId);

        Task<ApiResponse> GetSchedulePaperCandidateByRegistrationNumberAsync(long registrationNumber);

        Task<ApiResponse> UpdateSchedulePaperCandidateAsync(UpdateSchedulePaperCandidateRequestDto request);
    }
}
