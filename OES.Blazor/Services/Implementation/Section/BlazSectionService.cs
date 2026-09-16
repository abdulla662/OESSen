using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Section;
using OES.Helper.Dtos.Section;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Section
{
    public class BlazSectionService : IBlazSectionService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazSectionService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<ApiResponse> EditSectionAsync(EditSectionDto sectionDto)
        {
            return await _httpClientHelper.PostAsync(sectionDto, "api/Section/EditSection");
        }
    }
}
