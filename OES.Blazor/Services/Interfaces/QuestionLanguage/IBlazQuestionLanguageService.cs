using OES.Helper.Dtos.Questionlanguage;

namespace OES.Blazor.Services.Interfaces.Questionlanguage
{
    public interface IBlazQuestionLanguageService
    {
        Task<List<LanguageDto>> GetAllLanguagesAsync();

        Task<List<LanguageDto>> GetAllQuestionDetailsLanguagesGroupAsync(long questionMetadataId);
    }
}