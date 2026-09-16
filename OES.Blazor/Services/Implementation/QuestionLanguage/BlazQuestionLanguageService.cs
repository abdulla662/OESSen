using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.Questionlanguage;
using System.Net;

namespace OES.Blazor.Services.Implementation.QuestionLanguage
{
    public class BlazQuestionLanguageService : IBlazQuestionLanguageService
    {
        private readonly IHttpClientHelper _httpClient;

        public BlazQuestionLanguageService(IHttpClientHelper httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<LanguageDto>> GetAllLanguagesAsync()
        {
            var response = await _httpClient.GetAsync<List<LanguageDto>>("api/Language/GetAllLanguagesList");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return response.Data as List<LanguageDto>;
            }

            return [];
        }

        public async Task<List<LanguageDto>> GetAllQuestionDetailsLanguagesGroupAsync(long questionMetadataId)
        {
            var response = await _httpClient.GetAsync<List<LanguageDto>>($"api/Language/GetAllQuestionDetailsLanguagesGroup?questionMetadataId={questionMetadataId}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return response.Data as List<LanguageDto>;
            }

            return [];
        }
    }
}
