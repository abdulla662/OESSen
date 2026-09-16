using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.CBTSyncSettingService;
using OES.Blazor.Services.Interfaces.Common;
using OES.Helper.Dtos.CBTSyncSetting.Requests;
using OES.Helper.Dtos.CBTSyncSetting.Responses;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.CBTSyncSettingService
{
    public class BlazCBTSyncSettingService(
        IHttpClientHelper _httpClientHelper,
        IBlazGetCustomTableData<CBTSyncSettingResponseDto> _blazGetPaginationDto
    ) : IBlazCBTSyncSettingService
    {
        public async Task<CustomTableData<CBTSyncSettingResponseDto>> GetPaginatedAsync(PaginationSearchModel pagination)
            => await _blazGetPaginationDto.GetCustomTableData(pagination, "api/CBTSyncSetting/GetPaginatedCBTSyncSettings");

        public async Task<ApiResponse> GetByIdAsync(long id)
            => await _httpClientHelper.GetAsync<CBTSyncSettingResponseDto>($"api/CBTSyncSetting/GetCBTSyncSettingById?{nameof(id)}={id}");

        public async Task<ApiResponse> UpdateAsync(CBTSyncSettingRequestDto requestDto)
            => await _httpClientHelper.PostAsync(requestDto, "api/CBTSyncSetting/UpdateCBTSyncSetting");
    }
}
