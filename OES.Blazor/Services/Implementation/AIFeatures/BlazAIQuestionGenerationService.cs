using Microsoft.AspNetCore.Components.Forms;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.AIFeatures;
using OES.Helper.Dtos.AIQuestionGenerator.Request;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.AIFeatures
{
    public class BlazAIQuestionGenerationService : IBlazAIQuestionGenerationService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazAIQuestionGenerationService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<ApiResponse> GenerateQuestionsFromFileAsync(IBrowserFile file, AIQuestionGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default)
        {
            return await _httpClientHelper.PostFileWithJsonAsync(file, configuration, "api/AIQuestionGenerator/GenerateQuestionsFromFile", cancellationToken: cancellationToken);
        }

        public async Task<ApiResponse> GenerateQuestionsFromTextAsync(string documentText, AIQuestionGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default)
        {
            var request = new { DocumentText = documentText, Configuration = configuration };

            return await _httpClientHelper.PostAsync(request, "api/AIQuestionGenerator/GenerateQuestionsFromText", cancellationToken: cancellationToken);
        }

        public async Task<ApiResponse> SaveGeneratedQuestionsAsync(AIGeneratedQuestionsResultDto request, CancellationToken cancellationToken = default)
        {
            return await _httpClientHelper.PostAsync(request, "api/AIQuestionGenerator/SaveGeneratedQuestions", cancellationToken: cancellationToken);
        }
    }
}
