using Microsoft.AspNetCore.Mvc;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Interface.Interfaces;
using SharedHelper.Enums;

namespace OES.API.Controllers
{
    public class SectionSummaryController(ISectionSummaryService _sectionSummaryService) : OESBaseController
    {
        [HttpGet("GetSectionSummary")]
        public async Task<ApiResponse> GetSectionSummary(long paperId, QuestionSelectionType questionSelectionType, PaperType paperType)
        {
            return await _sectionSummaryService.GetSectionSummaryAsync(paperId, questionSelectionType, paperType);
        }
    }
}
