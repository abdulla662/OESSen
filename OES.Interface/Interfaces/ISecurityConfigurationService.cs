using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.ScheduleSecurityConfiguration;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface ISecurityConfigurationService
    {
        Task<IApiResponse> GetAllSecurityConfigurationsTemplatesAsync(PaginationSearchModel paginationSearchModel);

        Task<IApiResponse> GetSecurityConfigurationsTemplateByIdAsync(long securityTemplateId);

        Task<IApiResponse> GetSecurityConfigurationsByScheduleIdAsync(long scheduleId);

        Task<IApiResponse> AddSecurityConfigurationsAsync(AddOrUpdateSecurityConfigurationDto addSecurityConfigurationDto);

        Task<IApiResponse> UpdateSecurityConfigurationsAsync(AddOrUpdateSecurityConfigurationDto updateSecurityConfigurationDto);

        Task<IApiResponse> AddSecurityConfigurationsTemplateAsync(ScheduleSecurityConfigTempRequestDto configTempRequestDto);

        Task<IApiResponse> EditSecurityConfigurationsTemplateAsync(ScheduleSecurityConfigTempRequestDto scheduleSecurityConfigTempRequestDto);

        Task<IApiResponse> DeleteSecurityConfigurationsTemplateAsync(long templateId);
    }
}