using OES.Helper.Dtos.CBTSyncSetting.Requests;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface ICBTSyncSettingService
    {
        Task<ApiResponse> GetPaginatedAsync(PaginationSearchModel pagination);

        Task<ApiResponse> GetByIdAsync(long id);

        Task<ApiResponse> UpdateAsync(CBTSyncSettingRequestDto requestDto);
    }
}
