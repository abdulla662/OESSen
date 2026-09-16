using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Section;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class SectionController : OESBaseController
    {
        private readonly ISectionService _sectionService;

        public SectionController(ISectionService sectionService)
        {
            _sectionService = sectionService;
        }

        [HttpPost("EditSection")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> EditFormAsync(EditSectionDto sectionDto)
        {
            return await _sectionService.EditSectionAsync(sectionDto);
        }
    }
}
