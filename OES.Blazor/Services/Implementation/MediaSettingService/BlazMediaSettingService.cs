using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.MediaSettingService;
using OES.Helper.Dtos.MediaSetting.Requests;
using OES.Helper.Dtos.MediaSetting.Responses;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.MediaSettingService
{
    public class BlazMediaSettingService(IHttpClientHelper _httpClientHelper, IBlazGetCustomTableData<MediaSettingResponseDto> _blazGetMediaPaginationDto) : IBlazMediaSettingService
    {
        public async Task<CustomTableData<MediaSettingResponseDto>> GetPaginatedMediaSettingsAsync(PaginationSearchModel pagination)
        {
            return await _blazGetMediaPaginationDto.GetCustomTableData(pagination, "api/MediaSetting/GetPaginatedMediaSettings");
        }

        public async Task<ApiResponse> GetByIdAsync(long id)
        {
            return await _httpClientHelper.GetAsync<MediaSettingResponseDto>($"api/MediaSetting/GetMediaById?{nameof(id)}={id}");
        }

        public async Task<ApiResponse> GetByMediaCategoryAsync(MediaCategory category)
        {
            return await _httpClientHelper.GetAsync<MediaSettingResponseDto>($"api/MediaSetting/GetMediaByCategory?{nameof(category)}={category}");
        }

        public async Task<ApiResponse> AddMediaAsync(MediaSettingRequestDto requestDto)
        {
            return await _httpClientHelper.PostAsync(requestDto, "api/MediaSetting/AddMedia");
        }

        public async Task<ApiResponse> UpdateMediaAsync(MediaSettingRequestDto requestDto)
        {
            return await _httpClientHelper.PutAsync(requestDto, "api/MediaSetting/UpdateMedia");
        }

        public async Task<ApiResponse> DeleteAsync(long id)
        {
            return await _httpClientHelper.DeleteAsync($"api/MediaSetting/DeleteMedia?{nameof(id)}={id}");
        }

        //public async Task<List<GetOESGroupDto>> GetUserMediaSettingsGroupsAsync()
        //{
        //    var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/MediaSetting/GetUserMediaSettingsGroupsAsync");
        //    return (List<GetOESGroupDto>)response.Data ?? [];
        //}

        //public async Task<MediaSettingsGroupDto> GetMediaSettingsGroupsAsync(long mediaConfigurationId)
        //{
        //    var response = await _httpClientHelper.GetAsync<MediaSettingsGroupDto>($"api/MediaSetting/GetMediaSettingsGroupsAsync?mediaConfigurationId={mediaConfigurationId}");
        //    return response.Data as MediaSettingsGroupDto ?? new MediaSettingsGroupDto();
        //}
    }
}
