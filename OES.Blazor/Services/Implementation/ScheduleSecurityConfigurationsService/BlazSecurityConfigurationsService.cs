using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.ScheduleSecurityConfigurationsService;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.ScheduleSecurityConfiguration;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.ScheduleSecurityConfigurationsService
{
    public class BlazSecurityConfigurationsService : IBlazSecurityConfigurationsService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IBlazGetCustomTableData<SecurityConfigurationTemplateDto> _blazGetSecurityConfigurationTemplateDto;

        public BlazSecurityConfigurationsService(IHttpClientHelper httpClientHelper, IBlazGetCustomTableData<SecurityConfigurationTemplateDto> blazGetSecurityConfigurationTemplate)
        {
            _httpClientHelper = httpClientHelper;
            _blazGetSecurityConfigurationTemplateDto = blazGetSecurityConfigurationTemplate;
        }

        public async Task<CustomTableData<SecurityConfigurationTemplateDto>> GetAllSecurityConfigurationsTemplatesAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _blazGetSecurityConfigurationTemplateDto.GetCustomTableData(paginationSearchModel, "api/SecurityConfigurations/GetAllSecurityConfigurationsTemplates");
        }

        public async Task<ApiResponse> GetSecurityConfigurationsTemplateByIdAsync(long securityTemplateId)
        {
            return await _httpClientHelper.GetAsync<ScheduleSecConfigResponseDto>($"api/SecurityConfigurations/GetSecurityConfigurationsTemplateById?{nameof(securityTemplateId)}={securityTemplateId}");
        }

        public async Task<ExamSecurityConfigurationDto> GetSecurityConfigurationsByScheduleIdAsync(long scheduleId)
        {
            var responce = await _httpClientHelper.GetAsync<ExamSecurityConfigurationDto>($"api/SecurityConfigurations/GetSecurityConfigurationsByScheduleId?{nameof(scheduleId)}={scheduleId}");

            return (ExamSecurityConfigurationDto)responce.Data;
        }

        public async Task<ApiResponse> AddSecurityConfigurationsAsync(AddOrUpdateSecurityConfigurationDto addSecurityConfigurationDto)
        {
            return await _httpClientHelper.PostAsync(addSecurityConfigurationDto, "api/SecurityConfigurations/AddSecurityConfigurations");
        }

        public async Task<ApiResponse> UpdateSecurityConfigurationsAsync(AddOrUpdateSecurityConfigurationDto updateSecurityConfigurationDto)
        {
            return await _httpClientHelper.PostAsync(updateSecurityConfigurationDto, "api/SecurityConfigurations/UpdateSecurityConfigurations");
        }

        public async Task<ApiResponse> AddSecurityConfigurationsTemplateAsync(ScheduleSecurityConfigTempRequestDto scheduleSecurityConfigTempRequestDto)
        {
            return await _httpClientHelper.PostAsync(scheduleSecurityConfigTempRequestDto, "api/SecurityConfigurations/AddSecurityConfigurationsTemplate");
        }

        public async Task<ApiResponse> EditSecurityConfigurationsTemplateAsync(ScheduleSecurityConfigTempRequestDto scheduleSecurityConfigTempRequestDto)
        {
            return await _httpClientHelper.PostAsync(scheduleSecurityConfigTempRequestDto, "api/SecurityConfigurations/EditSecurityConfigurationsTemplate");
        }

        public async Task<ApiResponse> DeleteSecurityConfigurationsTemplateAsync(long templateId)
        {
            var response = await _httpClientHelper.DeleteAsync($"api/SecurityConfigurations/DeleteSecurityConfigurationsTemplate?{nameof(templateId)}={templateId}");

            return response;
        }
    }
}