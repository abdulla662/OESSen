using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface ILanguageService
    {
        Task<ApiResponse> GetLanguageByIdAsync(long id);

        Task<ApiResponse> GetAllLanguagesAsync();

        Task<ApiResponse> GetAllPaginatedLanguagesAsync(PaginationSearchModel pagination);

        Task<ApiResponse> GetAllQuestionDetailsLanguagesGroupAsync(long questionMetadataId);

        Task<ApiResponse> AddLanguageAsync(LanguageCreateDto languageCreateDto);

        Task<ApiResponse> UpdateLanguageAsync(LanguageUpdateDto languageUpdateDto);

        Task<ApiResponse> SoftDeleteLanguageAsync(long id);

        //Task<ApiResponse> GetUserLanguageGroupsAsync();

        //Task<ApiResponse> GetLanguageGroupsAsync(long languageId);
    }
}