using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.MediaSetting.Requests;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class MediaSettingController : OESBaseController
    {
        private readonly IMediaSettingService _mediaSettingService;

        public MediaSettingController(IMediaSettingService mediaSettingService)
        {
            _mediaSettingService = mediaSettingService;
        }

        [HttpPost("GetPaginatedMediaSettings")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetPaginatedMediaSettings(PaginationSearchModel pagination)
        {
            return await _mediaSettingService.GetPaginatedMediaSettings(pagination);
        }

        [HttpGet("GetMediaById")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetById(long id)
        {
            return await _mediaSettingService.GetByIdAsync(id);
        }

        [HttpGet("GetMediaByCategory")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetByCategory(MediaCategory category)
        {
            return await _mediaSettingService.GetByMediaCategoryAsync(category);
        }

        [HttpPost("AddMedia")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddMediaAsync(MediaSettingRequestDto requestDto)
        {
            return await _mediaSettingService.AddMediaAsync(requestDto);
        }

        [HttpPut("UpdateMedia")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateMediaAsync(MediaSettingRequestDto requestDto)
        {
            return await _mediaSettingService.UpdateMediaAsync(requestDto);
        }

        [HttpDelete("DeleteMedia")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> Delete(long id)
        {
            return await _mediaSettingService.DeleteAsync(id);
        }

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetUserMediaSettingsGroupsAsync")]
        //public async Task<ApiResponse> GetUserMediaSettingsGroupsAsync()
        //{
        //    return await _mediaSettingService.GetUserMediaSettingsGroupsAsync();
        //}

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetMediaSettingsGroupsAsync")]
        //public async Task<IApiResponse> GetMediaSettingsGroupsAsync(long mediaSettingsId)
        //{
        //    return await _mediaSettingService.GetMediaSettingsGroupsAsync(mediaSettingsId);
        //}
    }
}
