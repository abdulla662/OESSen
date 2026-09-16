using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.CBTSyncSetting.Requests;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class CBTSyncSettingController : OESBaseController
    {
        private readonly ICBTSyncSettingService _cbtSyncSettingService;

        public CBTSyncSettingController(ICBTSyncSettingService cbtSyncSettingService)
        {
            _cbtSyncSettingService = cbtSyncSettingService;
        }

        [HttpPost("GetPaginatedCBTSyncSettings")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetPaginatedCBTSyncSettings(PaginationSearchModel pagination)
            => await _cbtSyncSettingService.GetPaginatedAsync(pagination);

        [HttpGet("GetCBTSyncSettingById")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetCBTSyncSettingById(long id)
            => await _cbtSyncSettingService.GetByIdAsync(id);

        [HttpPost("UpdateCBTSyncSetting")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateCBTSyncSetting(CBTSyncSettingRequestDto requestDto)
            => await _cbtSyncSettingService.UpdateAsync(requestDto);
    }
}
