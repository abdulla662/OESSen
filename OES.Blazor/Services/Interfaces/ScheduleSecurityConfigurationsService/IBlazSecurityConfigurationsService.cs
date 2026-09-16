using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.ScheduleSecurityConfiguration;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.ScheduleSecurityConfigurationsService
{
    public interface IBlazSecurityConfigurationsService
    {
        Task<CustomTableData<SecurityConfigurationTemplateDto>> GetAllSecurityConfigurationsTemplatesAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetSecurityConfigurationsTemplateByIdAsync(long securityTemplateId);

        Task<ExamSecurityConfigurationDto> GetSecurityConfigurationsByScheduleIdAsync(long scheduleId);

        Task<ApiResponse> AddSecurityConfigurationsAsync(AddOrUpdateSecurityConfigurationDto addSecurityConfigurationDto);

        Task<ApiResponse> UpdateSecurityConfigurationsAsync(AddOrUpdateSecurityConfigurationDto updateSecurityConfigurationDto);

        Task<ApiResponse> AddSecurityConfigurationsTemplateAsync(ScheduleSecurityConfigTempRequestDto scheduleSecurityConfigTempRequestDto);

        Task<ApiResponse> EditSecurityConfigurationsTemplateAsync(ScheduleSecurityConfigTempRequestDto scheduleSecurityConfigTempRequestDto);

        Task<ApiResponse> DeleteSecurityConfigurationsTemplateAsync(long templateId);
    }
}