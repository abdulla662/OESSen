using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Interface.Interfaces
{
    public interface ISeederService
    {
        Task<ApiResponse> SeedPages(List<PageDTO> ListOfPages);
        Task<bool> SeedApiEndpoints();
        Task SeedPredefinedTemplatesAsync();
        Task<ApiResponse> SeedQuestionTypes();
        Task ApplyQuestionTypeExclusionsAsync();
        Task<ApiResponse> SeedQuestionLayout();
        Task<bool> SeedItemBankLevel();
        Task<bool> SeedSuperAdmin();
        Task<bool> SeedTemplateTypesAttributesAsync();
        Task<bool> SeedDifficultyProfileWithDifficultyLevels();
        Task<bool> SeedSubjects();
        Task<bool> SeedLanguages();
        Task<ApiResponse> SeedMediaSettings();
        Task SeedCBTSyncSettingAsync();
        Task SeedPageRolesAsync();
        Task SeedApiEndpointRolesAsync();
    }
}
