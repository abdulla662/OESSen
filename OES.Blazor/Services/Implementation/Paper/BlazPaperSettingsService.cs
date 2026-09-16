using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.PaperSetting.Requests;
using OES.Helper.Dtos.PaperSetting.Responses;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Paper
{
    public class BlazPaperSettingsService(IHttpClientHelper _httpClient, IBlazGetCustomTableData<PaperSettingsTemplatePaginated> _blazGetCustomTableData) : IBlazPaperSettingsService
    {
        public async Task<CustomTableData<PaperSettingsTemplatePaginated>> GetAllPaperSettingsTemplatesPaginatedAsync(PaginationSearchModel paginationSearchModel)
        {
            var data = await _blazGetCustomTableData.GetCustomTableData(paginationSearchModel, "api/PaperSettings/GetAllPaperSettingsTemplatesPaginated");

            return data;
        }

        public async Task<ApiResponse> GetPaperSettingsTemplateByIdAsync(long id)
        {
            return await _httpClient.GetAsync<PaperSettingsTemplateRequestDto>($"api/PaperSettings/GetPaperSettingsTemplateById?{nameof(id)}={id}");
        }

        public async Task<GetPaperSettingsResponseDto> GetPaperSettingsBySchedulePaperIdAsync(long schedulePaperId)
        {
            var response = await _httpClient.GetAsync<GetPaperSettingsResponseDto>($"api/PaperSettings/GetPaperSettingsBySchedulePaperId?{nameof(schedulePaperId)}={schedulePaperId}");

            return (GetPaperSettingsResponseDto)response.Data;
        }

        public async Task<ApiResponse> AddPaperSettingsAsync(AddOrUpdatePaperSettingsRequestDto addPaperSettingsRequestDto)
        {
            return await _httpClient.PostAsync(addPaperSettingsRequestDto, "api/PaperSettings/AddPaperSettings");
        }

        public async Task<ApiResponse> UpdatePaperSettingsAsync(AddOrUpdatePaperSettingsRequestDto updatePaperSettingsRequestDto)
        {
            return await _httpClient.PostAsync(updatePaperSettingsRequestDto, "api/PaperSettings/UpdatePaperSettings");
        }

        public async Task<ApiResponse> AddPaperSettingsTemplateAsync(PaperSettingsTemplateRequestDto addPaperSettingsTemplateRequestDto)
        {
            return await _httpClient.PostAsync(addPaperSettingsTemplateRequestDto, "api/PaperSettings/AddPaperSettingsTemplate");
        }

        public async Task<ApiResponse> UpdatePaperSettingsTemplateAsync(PaperSettingsTemplateRequestDto updatePaperSettingsTemplateRequestDto)
        {
            return await _httpClient.PostAsync(updatePaperSettingsTemplateRequestDto, "api/PaperSettings/UpdatePaperSettingsTemplate");
        }

        public async Task<ApiResponse> DeletePaperSettingsTemplateAsync(long id)
        {
            return await _httpClient.DeleteAsync($"api/PaperSettings/DeletePaperSettingsTemplate?{nameof(id)}={id}");
        }
    }
}
