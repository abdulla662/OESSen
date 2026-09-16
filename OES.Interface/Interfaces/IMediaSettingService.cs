using OES.Helper.Dtos.MediaSetting.Requests;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IMediaSettingService
    {
        Task<ApiResponse> GetPaginatedMediaSettings(PaginationSearchModel pagination);
        Task<ApiResponse> GetByIdAsync(long id);
        Task<ApiResponse> GetByMediaCategoryAsync(MediaCategory mediaCategory);
        Task<ApiResponse> AddMediaAsync(MediaSettingRequestDto requestDto);
        Task<ApiResponse> UpdateMediaAsync(MediaSettingRequestDto requestDto);
        Task<ApiResponse> DeleteAsync(long id);
        //Task<ApiResponse> GetUserMediaSettingsGroupsAsync();
        //Task<ApiResponse> GetMediaSettingsGroupsAsync(long mediasettingsId);
    }
}
