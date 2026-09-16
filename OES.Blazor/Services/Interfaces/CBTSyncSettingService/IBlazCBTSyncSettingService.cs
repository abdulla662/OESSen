using OES.Helper.Dtos.CBTSyncSetting.Requests;
using OES.Helper.Dtos.CBTSyncSetting.Responses;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.CBTSyncSettingService
{
    public interface IBlazCBTSyncSettingService
    {
        Task<CustomTableData<CBTSyncSettingResponseDto>> GetPaginatedAsync(PaginationSearchModel pagination);

        Task<ApiResponse> GetByIdAsync(long id);

        Task<ApiResponse> UpdateAsync(CBTSyncSettingRequestDto requestDto);
    }
}
