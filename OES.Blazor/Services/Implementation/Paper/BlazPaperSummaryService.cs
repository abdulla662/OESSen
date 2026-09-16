using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.Paper.Responses.AdaptiveSummary;
using OES.Helper.Enums;
using OES.Helper.General;
using SharedHelper.Enums;

namespace OES.Blazor.Services.Implementation.Paper
{
    public class BlazPaperSummaryService(IHttpClientHelper _httpClient) : IBlazPaperSummaryService
    {
        public async Task<ApiResponse> GetAdaptiveSectionSummary(long paperId, QuestionSelectionType questionSelectionType, PaperType paperType)
        {
            return await _httpClient.GetAsync<PaperStageSummaryResponseDto>($"api/SectionSummary/GetSectionSummary?paperId={paperId}&questionSelectionType={questionSelectionType}&paperType={paperType}");
        }

        public async Task<ApiResponse> GetStandardSectionSummaryAsync<TDto>(long paperId, QuestionSelectionType questionSelectionType, PaperType paperType)
        {
            return await _httpClient.GetAsync<TDto>($"api/SectionSummary/GetSectionSummary?{nameof(paperId)}={paperId}&{nameof(questionSelectionType)}={questionSelectionType}&{nameof(paperType)}={paperType}");
        }
    }
}
