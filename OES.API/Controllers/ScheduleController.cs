using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class ScheduleController(IScheduleService _scheduleService) : OESBaseController
    {
        [HttpPost("GetAllScheduleAsync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllScheduleAsync(PaginationSearchModel paginationModel)
        {
            return await _scheduleService.GetAllScheduleAsync(paginationModel);
        }

        [HttpPost("GetAllUnexpiredPublishedSchedulesPaginated")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllUnexpiredPublishedSchedulesPaginatedAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _scheduleService.GetAllUnexpiredPublishedSchedulesPaginatedAsync(paginationSearchModel);
        }

        [HttpGet("GetScheduleByIdAsync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetScheduleByIdAsync(long id)
        {
            return await _scheduleService.GetScheduleByIdAsync(id);
        }

        [HttpGet("GetScheduleValidationParameters")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetScheduleValidationParametersAsync(long scheduleMetadataId)
        {
            return await _scheduleService.GetScheduleValidationParametersAsync(scheduleMetadataId);
        }

        [HttpPost("AddScheduleMetadata")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddScheduleMetadataAsync(AddScheduleMetadataRequestDto addScheduleMetadataRequestDto)
        {
            return await _scheduleService.AddScheduleMetadataAsync(addScheduleMetadataRequestDto);
        }

        [HttpPut("UpdateScheduleMetadata")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateScheduleMetadataAsync(UpdateScheduleMetadataRequestDto updateScheduleMetadataRequestDto)
        {
            return await _scheduleService.UpdateScheduleMetadataAsync(updateScheduleMetadataRequestDto);
        }

        [HttpDelete("DeleteScheduleAsync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DeleteScheduleAsync(long scheduleId)
        {
            return await _scheduleService.DeleteScheduleAsync(scheduleId);
        }

        [HttpGet("PublishSchedule")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> PublishScheduleAsync(long scheduleMetadataId)
        {
            return await _scheduleService.PublishScheduleAsync(scheduleMetadataId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("CopyScheduleAsync")]
        public async Task<ApiResponse> CopyPaperAsync(long scheduleId)
        {
            return await _scheduleService.CopyScheduleAsync(scheduleId);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetAllScheduleTemplates")]
        public async Task<ApiResponse> GetAllScheduleTemplates(PaginationSearchModel paginationSearchModel)
        {
            return await _scheduleService.GetAllScheduleTemplatesAsync(paginationSearchModel);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetScheduleTemplateById")]
        public async Task<ApiResponse> GetScheduleTemplateById(long scheduleTemplateId)
        {
            return await _scheduleService.GetScheduleTemplateByIdAsync(scheduleTemplateId);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("AddScheduleTemplate")]
        public async Task<ApiResponse> AddScheduleTemplateAsync(ScheduleCreationTemplateDto scheduleCreationTemplateDto)
        {
            return await _scheduleService.AddScheduleTemplateAsync(scheduleCreationTemplateDto);
        }

        [OESFilter(Authorize = true)]
        [HttpDelete("DeleteScheduleMetadataTemplate")]
        public async Task<ApiResponse> DeleteScheduleMetadataTemplateAsync(long templateId)
        {
            return await _scheduleService.DeleteScheduleMetadataTemplateAsync(templateId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetScheduleGroups")]
        public async Task<ApiResponse> GetScheduleGroupsAsync(long scheduleId)
        {
            return await _scheduleService.GetScheduleGroupsAsync(scheduleId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetAllSchedulesCreatedByCurrentUser")]
        public async Task<ApiResponse> GetAllSchedulesCreatedByCurrentUser()
        {
            return await _scheduleService.GetAllSchedulesCreatedByCurrentUser();
        }

        [HttpPut("ToggleAutoSync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> ToggleAutoSyncAsync(long scheduleId, bool isEnabled)
        {
            return await _scheduleService.ToggleAutoSyncAsync(scheduleId, isEnabled);
        }

        [HttpGet("GetAutoSyncStatus")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAutoSyncStatusAsync(long scheduleId)
        {
            return await _scheduleService.GetAutoSyncStatusAsync(scheduleId);
        }
    }
}