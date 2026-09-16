using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Schedule
{
    public interface IBlazScheduleService
    {
        Task<CustomTableData<ScheduleMetadataPaginationDto>> GetAllSchedulesAsync(PaginationSearchModel pagination);

        Task<CustomTableData<ScheduleMetadataPaginationDto>> GetAllUnexpiredPublishedSchedulesPaginatedAsync(PaginationSearchModel paginationSearchModel);

        Task<ScheduleMetadataDto> GetScheduleByIdAsync(long id);

        Task<ScheduleGroupsDto> GetScheduleGroupsAsync(long scheduleId);

        Task<ScheduleValidationParametersResponseDto> GetScheduleValidationParametersAsync(long scheduleMetadataId);

        Task<List<GetOESGroupDto>> GetAllSchedulesCreatedByCurrentUser();

        Task<ApiResponse> AddScheduleMetadataAsync(AddScheduleMetadataRequestDto addScheduleMetadataRequestDto);

        Task<ApiResponse> UpdateScheduleMetadataAsync(UpdateScheduleMetadataRequestDto updateScheduleMetadataRequestDto);

        Task<ApiResponse> DeleteScheduleAsync(long scheduleId);

        Task<ApiResponse> PublishScheduleAsync(long scheduleMetadataId);

        Task<ApiResponse> CopyScheduleAsync(long scheduleId);

        Task<CustomTableData<ScheduleTemplateDto>> GetAllScheduleTemplates(PaginationSearchModel paginationSearch);

        Task<ApiResponse> GetScheduleTemplateByIdAsync(long scheduleTemplateId);

        Task<ApiResponse> AddScheduleTemplateAsync(ScheduleCreationTemplateDto scheduleTemplateDto);

        Task<ApiResponse> DeleteScheduleMetadataTemplateAsync(long templateId);
    }
}