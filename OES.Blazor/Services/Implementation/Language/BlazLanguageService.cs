using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Language;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Language
{
    public class BlazLanguageService : IBlazLanguageService
    {
        private readonly IBlazGetCustomTableData<LanguageDto> _blazGetCustomTable;
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazLanguageService(IBlazGetCustomTableData<LanguageDto> blazGetCustomTable,
                                   IHttpClientHelper httpClientHelper)
        {
            _blazGetCustomTable = blazGetCustomTable;
            _httpClientHelper = httpClientHelper;
        }

        public Task<CustomTableData<LanguageDto>> GetAllLanguages(PaginationSearchModel pagination)
        {
            return _blazGetCustomTable.GetCustomTableData(pagination, "api/Language/GetPaginatedLanguagesList");
        }

        public async Task<GetLanguageDto> GetLanguageByIdAsync(long id)
        {
            var response = await _httpClientHelper.GetAsync<GetLanguageDto>($"api/Language/GetLanguageById?id={id}");

            return (GetLanguageDto)response.Data;
        }

        public async Task<ApiResponse> AddLanguage(LanguageCreateDto dto)
        {
            return await _httpClientHelper.PostAsync(dto, "api/Language/AddLanguage");
        }

        public async Task<ApiResponse> UpdateLanguageAsync(LanguageUpdateDto languageUpdateDto)
        {
            return await _httpClientHelper.PutAsync(languageUpdateDto, "api/Language/UpdateLanguage");
        }

        public async Task<ApiResponse> SoftDeleteLanguage(long id)
        {
            return await _httpClientHelper.DeleteAsync($"api/Language/DeleteLanguage?id={id}");
        }

        //public async Task<List<GetOESGroupDto>> GetUserLanguageGroupsAsync()
        //{
        //    var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/Language/GetUserLanguageGroupsAsync");
        //    return (List<GetOESGroupDto>)response.Data ?? [];
        //}

        //public async Task<LanguageGroupDto> GetLanguageGroupsAsync(long languageId)
        //{
        //    var response = await _httpClientHelper.GetAsync<LanguageGroupDto>($"api/Language/GetLanguageGroupsAsync?languageId={languageId}");
        //    return response.Data as LanguageGroupDto ?? new LanguageGroupDto();
        //}
    }
}
