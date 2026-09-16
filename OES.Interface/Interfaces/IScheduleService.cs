using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IScheduleService
    {
        Task<ApiResponse> GetAllScheduleAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetAllUnexpiredPublishedSchedulesPaginatedAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetScheduleByIdAsync(long id);

        Task<ApiResponse> GetScheduleValidationParametersAsync(long scheduleMetadataId);

        Task<ApiResponse> GetScheduleGroupsAsync(long scheduleId);

        Task<ApiResponse> AddScheduleMetadataAsync(AddScheduleMetadataRequestDto addScheduleMetadataRequestDto);

        Task<ApiResponse> UpdateScheduleMetadataAsync(UpdateScheduleMetadataRequestDto updateScheduleMetadataRequestDto);

        Task<ApiResponse> DeleteScheduleAsync(long scheduleId);

        Task<ApiResponse> PublishScheduleAsync(long scheduleMetadataId);

        Task<ApiResponse> CopyScheduleAsync(long scheduleId);

        Task<ApiResponse> GetAllScheduleTemplatesAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetAllSchedulesCreatedByCurrentUser();

        Task<ApiResponse> GetScheduleTemplateByIdAsync(long scheduleTemplateId);

        Task<ApiResponse> AddScheduleTemplateAsync(ScheduleCreationTemplateDto scheduleTemplateDto);

        Task<ApiResponse> DeleteScheduleMetadataTemplateAsync(long templateId);

        Task<ApiResponse> UpdateScheduleSyncStatus(long scheduleId);

        Task<ApiResponse> ToggleAutoSyncAsync(long scheduleId, bool isEnabled);

        Task<ApiResponse> GetAutoSyncStatusAsync(long scheduleId);
    }
}