using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.PaperSetting.Requests;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class PaperSettingsController : OESBaseController
    {
        private readonly IPaperSettingsService _paperSettingService;


        public PaperSettingsController(IPaperSettingsService paperSettingService)
        {
            _paperSettingService = paperSettingService;
        }


        [HttpPost("GetAllPaperSettingsTemplatesPaginated")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllPaperSettingsTemplatesPaginatedAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _paperSettingService.GetAllPaperSettingsTemplatesPaginatedAsync(paginationSearchModel);
        }


        [HttpGet("GetPaperSettingsTemplateById")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetPaperSettingsTemplateByIdAsync(long id)
        {
            return await _paperSettingService.GetPaperSettingsTemplateByIdAsync(id);
        }


        [HttpGet("GetPaperSettingsBySchedulePaperId")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetPaperSettingsBySchedulePaperIdAsync(long schedulePaperId)
        {
            return await _paperSettingService.GetPaperSettingsBySchedulePaperIdAsync(schedulePaperId);
        }


        [HttpPost("AddPaperSettings")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddPaperSettingsAsync(AddOrUpdatePaperSettingsRequestDto addPaperSettingsRequestDto)
        {
            return await _paperSettingService.AddPaperSettingsAsync(addPaperSettingsRequestDto);
        }


        [HttpPost("UpdatePaperSettings")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UpdatePaperSettingsAsync(AddOrUpdatePaperSettingsRequestDto updatePaperSettingsRequestDto)
        {
            return await _paperSettingService.UpdatePaperSettingsAsync(updatePaperSettingsRequestDto);
        }


        [HttpPost("AddPaperSettingsTemplate")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddPaperSettingsTemplateAsync(PaperSettingsTemplateRequestDto addPaperSettingsTemplateRequestDto)
        {
            return await _paperSettingService.AddPaperSettingsTemplateAsync(addPaperSettingsTemplateRequestDto);
        }


        [HttpPost("UpdatePaperSettingsTemplate")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UpdatePaperSettingsTemplateAsync(PaperSettingsTemplateRequestDto updatePaperSettingsTemplateRequestDto)
        {
            return await _paperSettingService.UpdatePaperSettingsTemplateAsync(updatePaperSettingsTemplateRequestDto);
        }


        [HttpDelete("DeletePaperSettingsTemplate")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> DeletePaperSettingsTemplateAsync(long id)
        {
            return await _paperSettingService.DeletePaperSettingsTemplateAsync(id);
        }
    }
}
