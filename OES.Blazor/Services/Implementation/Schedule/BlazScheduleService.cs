using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.General;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.Schedule
{
    public class BlazScheduleService : IBlazScheduleService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IBlazGetCustomTableData<ScheduleMetadataPaginationDto> _blazGetCustomTable;
        private readonly IBlazGetCustomTableData<ScheduleTemplateDto> _blazGetScheduleTemplateDto;

        public BlazScheduleService(IHttpClientHelper httpClientHelper, IBlazGetCustomTableData<ScheduleMetadataPaginationDto> blazGetCustomTable, IBlazGetCustomTableData<ScheduleTemplateDto> blazGetScheduleTemplateDto)
        {
            _httpClientHelper = httpClientHelper;
            _blazGetCustomTable = blazGetCustomTable;
            _blazGetScheduleTemplateDto = blazGetScheduleTemplateDto;
        }

        public Task<CustomTableData<ScheduleMetadataPaginationDto>> GetAllSchedulesAsync(PaginationSearchModel pagination)
        {
            return _blazGetCustomTable.GetCustomTableData(pagination, "api/Schedule/GetAllScheduleAsync");
        }

        public Task<CustomTableData<ScheduleMetadataPaginationDto>> GetAllUnexpiredPublishedSchedulesPaginatedAsync(PaginationSearchModel paginationSearchModel)
        {
            return _blazGetCustomTable.GetCustomTableData(paginationSearchModel, "api/Schedule/GetAllUnexpiredPublishedSchedulesPaginated");
        }

        public async Task<ScheduleMetadataDto> GetScheduleByIdAsync(long id)
        {
            var response = await _httpClientHelper.GetAsync<ScheduleMetadataDto>($"api/Schedule/GetScheduleByIdAsync?{nameof(id)}={id}");

            if (response?.Data is JsonElement json)
                return json.Deserialize<ScheduleMetadataDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ScheduleMetadataDto();

            return response?.Data as ScheduleMetadataDto ?? new ScheduleMetadataDto();
        }

        public async Task<ScheduleGroupsDto> GetScheduleGroupsAsync(long scheduleId)
        {
            var response = await _httpClientHelper.GetAsync<ScheduleGroupsDto>($"api/Schedule/GetScheduleGroups?{nameof(scheduleId)}={scheduleId}");

            return (ScheduleGroupsDto)response.Data;
        }

        public async Task<ScheduleValidationParametersResponseDto> GetScheduleValidationParametersAsync(long scheduleMetadataId)
        {
            var response = await _httpClientHelper.GetAsync<ScheduleValidationParametersResponseDto>($"api/Schedule/GetScheduleValidationParameters?{nameof(scheduleMetadataId)}={scheduleMetadataId}");

            return (ScheduleValidationParametersResponseDto)response.Data;
        }

        public async Task<List<GetOESGroupDto>> GetAllSchedulesCreatedByCurrentUser()
        {
            var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/Schedule/GetAllSchedulesCreatedByCurrentUser");

            var data = (List<GetOESGroupDto>)response.Data;

            return data ?? [];
        }

        public async Task<ApiResponse> AddScheduleMetadataAsync(AddScheduleMetadataRequestDto addScheduleMetadataRequestDto)
        {
            var response = await _httpClientHelper.PostAsync(addScheduleMetadataRequestDto, "api/Schedule/AddScheduleMetadata");

            return response;
        }

        public async Task<ApiResponse> UpdateScheduleMetadataAsync(UpdateScheduleMetadataRequestDto updateScheduleMetadataRequestDto)
        {
            return await _httpClientHelper.PutAsync(updateScheduleMetadataRequestDto, "api/Schedule/UpdateScheduleMetadata");
        }

        public async Task<ApiResponse> DeleteScheduleAsync(long scheduleId)
        {
            return await _httpClientHelper.DeleteAsync($"api/Schedule/DeleteScheduleAsync?{nameof(scheduleId)}={scheduleId}");
        }

        public async Task<ApiResponse> PublishScheduleAsync(long scheduleMetadataId)
        {
            return await _httpClientHelper.GetAsync<object>($"api/Schedule/PublishSchedule?{nameof(scheduleMetadataId)}={scheduleMetadataId}");
        }

        public async Task<ApiResponse> CopyScheduleAsync(long scheduleId)
        {
            var response = await _httpClientHelper.GetAsync<object>($"api/Schedule/CopyScheduleAsync?{nameof(scheduleId)}={scheduleId}");

            return response;
        }

        public async Task<CustomTableData<ScheduleTemplateDto>> GetAllScheduleTemplates(PaginationSearchModel paginationSearch)
        {
            var templates = await _blazGetScheduleTemplateDto.GetCustomTableData(paginationSearch, "api/Schedule/GetAllScheduleTemplates");

            return templates;
        }

        public async Task<ApiResponse> GetScheduleTemplateByIdAsync(long scheduleTemplateId)
        {
            var result = await _httpClientHelper.GetAsync<ScheduleMetadataRetrievalDto>($"api/Schedule/GetScheduleTemplateById?{nameof(scheduleTemplateId)}={scheduleTemplateId}");

            return result;
        }

        public async Task<ApiResponse> AddScheduleTemplateAsync(ScheduleCreationTemplateDto scheduleTemplateDto)
        {
            var response = await _httpClientHelper.PostAsync(scheduleTemplateDto, "api/Schedule/AddScheduleTemplate");

            return response;
        }

        public async Task<ApiResponse> DeleteScheduleMetadataTemplateAsync(long templateId)
        {
            var response = await _httpClientHelper.DeleteAsync($"api/Schedule/DeleteScheduleMetadataTemplate?{nameof(templateId)}={templateId}");

            return response;
        }
    }
}