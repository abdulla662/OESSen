using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.MarkingScheme;
using OES.Helper.Dtos.MarkingScheme;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.MarkingScheme
{
    public class BlazMarkingSchemeService(IHttpClientHelper _httpClientHelper) : IBlazMarkingSchemeService
    {
        public async Task<MarkingSchemeDto> GetPaperMarkingSchemeAsync(long paperId)
        {
            var apiResponse = await _httpClientHelper.GetAsync<MarkingSchemeDto>($"api/MarkingScheme/GetPaperMarkingScheme?{nameof(paperId)}={paperId}");

            var markingSchemeDto = (MarkingSchemeDto)apiResponse.Data ?? new();

            return markingSchemeDto;
        }

        public async Task<ApiResponse> AddOrUpdateMarkingSchemeAsync(MarkingSchemeDto markingSchemeDto)
        {
            return await _httpClientHelper.PostAsync(markingSchemeDto, "api/MarkingScheme/AddOrUpdateMarkingScheme");
        }
    }
}