using OES.Helper.Dtos.MediaSetting.Requests;
using OES.Helper.Dtos.MediaSetting.Responses;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.MediaSettingService
{
    public interface IBlazMediaSettingService
    {
        Task<CustomTableData<MediaSettingResponseDto>> GetPaginatedMediaSettingsAsync(PaginationSearchModel pagination);

        Task<ApiResponse> GetByIdAsync(long id);

        Task<ApiResponse> GetByMediaCategoryAsync(MediaCategory category);

        Task<ApiResponse> AddMediaAsync(MediaSettingRequestDto requestDto);

        Task<ApiResponse> UpdateMediaAsync(MediaSettingRequestDto requestDto);

        Task<ApiResponse> DeleteAsync(long id);

        //Task<List<GetOESGroupDto>> GetUserMediaSettingsGroupsAsync();

        //Task<MediaSettingsGroupDto> GetMediaSettingsGroupsAsync(long mediaConfigurationId);
    }
}
