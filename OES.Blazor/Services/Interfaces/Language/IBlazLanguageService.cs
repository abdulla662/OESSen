using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Language
{
    public interface IBlazLanguageService
    {
        Task<CustomTableData<LanguageDto>> GetAllLanguages(PaginationSearchModel pagination);

        Task<GetLanguageDto> GetLanguageByIdAsync(long id);

        Task<ApiResponse> AddLanguage(LanguageCreateDto dto);

        Task<ApiResponse> UpdateLanguageAsync(LanguageUpdateDto languageUpdateDto);

        Task<ApiResponse> SoftDeleteLanguage(long id);

        //Task<List<GetOESGroupDto>> GetUserLanguageGroupsAsync();

        //Task<LanguageGroupDto> GetLanguageGroupsAsync(long languageId);
    }
}
