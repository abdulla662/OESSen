using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.MarkingScheme;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class MarkingSchemeController(IMarkingSchemeService _markingSchemeService) : OESBaseController
    {
        [HttpGet("GetPaperMarkingScheme")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetPaperMarkingSchemeAsync(long paperId)
        {
            return await _markingSchemeService.GetPaperMarkingSchemeAsync(paperId);
        }

        [HttpPost("AddOrUpdateMarkingScheme")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddOrUpdateMarkingSchemeAsync(MarkingSchemeDto markingSchemeDto)
        {
            return await _markingSchemeService.AddOrUpdateMarkingSchemeAsync(markingSchemeDto);
        }
    }
}