using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.ScheduleSummary;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Schedule
{
    public interface IBlazSchedulePaperService
    {
        Task<CustomTableData<SchedulePaperPaginationDto>> GetAllSchedulePapersByScheduleIdAsync(PaginationSearchModel paginationSearchModel, long scheduleId);

        Task<GetSchedulePaperResponseDto> GetSchedulePaperByIdAsync(long schedulePaperId);

        Task<GetScheduleSummaryDto> GetSchedulePaperSummaryAsync(long scheduleId);

        Task<ApiResponse> AddSchedulePaperAsync(AddOrUpdateSchedulePaperRequestDto addSchedulePaperRequestDto);

        Task<ApiResponse> UpdateSchedulePaperAsync(AddOrUpdateSchedulePaperRequestDto updateSchedulePaperRequestDto);

        Task<ApiResponse> DeleteSchedulePaperAsync(long schedulePaperId);

        Task<ApiResponse> GetSchedulePaperAllocation(long schedulePaperId);

        Task<ApiResponse> DeletePaperAllocationAsync(long venueId, long schedulePaperId);

        Task<List<GetSchedulePaperTimeConfiguration>> GetSchedulePapersAsync(long scheduleMetadataId);

        Task<CustomTableData<VenueCandidatesCountDto>> GetVenuesWithCandidatesCountByPaperIdAsync(PaginationSearchModel paginationSearchModel, long paperId);

        Task<List<string>> GetSchedulePaperVenueCodesAsync(long schedulePaperId);

        Task ExportCandidatesToExcelAsync(long schedulePaperId);

        Task<ApiResponse> GetVenueBatchesAsync(long venueId, long paperId);

        Task<SchedulePaperCandidateDetailsDto> GetSchedulePaperCandidateByRegistrationNumberAsync(long registrationNumber);

        Task<ApiResponse> UpdateSchedulePaperCandidateAsync(UpdateSchedulePaperCandidateRequestDto request);
    }
}