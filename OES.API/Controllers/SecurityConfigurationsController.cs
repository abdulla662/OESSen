using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.ScheduleSecurityConfiguration;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class SecurityConfigurationsController : OESBaseController
    {
        private readonly ISecurityConfigurationService _securityConfigurationService;

        public SecurityConfigurationsController(ISecurityConfigurationService securityConfigurationService)
        {
            _securityConfigurationService = securityConfigurationService;
        }

        [HttpPost("GetAllSecurityConfigurationsTemplates")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetAllSecurityConfigurationsTemplatesAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _securityConfigurationService.GetAllSecurityConfigurationsTemplatesAsync(paginationSearchModel);
        }

        [HttpGet("GetSecurityConfigurationsTemplateById")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetSecurityConfigurationsTemplateByIdAsync(long securityTemplateId)
        {
            return await _securityConfigurationService.GetSecurityConfigurationsTemplateByIdAsync(securityTemplateId);
        }

        [HttpGet("GetSecurityConfigurationsByScheduleId")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetSecurityConfigurationsByScheduleIdAsync(long scheduleId)
        {
            return await _securityConfigurationService.GetSecurityConfigurationsByScheduleIdAsync(scheduleId);
        }

        [HttpPost("AddSecurityConfigurations")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddSecurityConfigurationsAsync(AddOrUpdateSecurityConfigurationDto addSecurityConfigurationDto)
        {
            return await _securityConfigurationService.AddSecurityConfigurationsAsync(addSecurityConfigurationDto);
        }

        [HttpPost("UpdateSecurityConfigurations")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UpdateSecurityConfigurations(AddOrUpdateSecurityConfigurationDto updateSecurityConfigurationDto)
        {
            return await _securityConfigurationService.UpdateSecurityConfigurationsAsync(updateSecurityConfigurationDto);
        }

        [HttpPost("AddSecurityConfigurationsTemplate")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddSecurityConfigurationsTemplateAsync(ScheduleSecurityConfigTempRequestDto scheduleSecurityConfigTempRequestDto)
        {
            return await _securityConfigurationService.AddSecurityConfigurationsTemplateAsync(scheduleSecurityConfigTempRequestDto);
        }

        [HttpPost("EditSecurityConfigurationsTemplate")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> EditSecurityConfigurationsTemplateAsync(ScheduleSecurityConfigTempRequestDto scheduleSecurityConfigTempRequestDto)
        {
            return await _securityConfigurationService.EditSecurityConfigurationsTemplateAsync(scheduleSecurityConfigTempRequestDto);
        }

        [HttpDelete("DeleteSecurityConfigurationsTemplate")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> DeleteSecurityConfigurationsTemplateAsync(long templateId)
        {
            return await _securityConfigurationService.DeleteSecurityConfigurationsTemplateAsync(templateId);
        }
    }
}