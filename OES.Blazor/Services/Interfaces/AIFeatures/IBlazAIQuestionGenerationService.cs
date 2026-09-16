using Microsoft.AspNetCore.Components.Forms;
using OES.Helper.Dtos.AIQuestionGenerator.Request;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.AIFeatures
{
    public interface IBlazAIQuestionGenerationService
    {
        Task<ApiResponse> GenerateQuestionsFromFileAsync(IBrowserFile file, AIQuestionGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default);

        Task<ApiResponse> GenerateQuestionsFromTextAsync(string documentText, AIQuestionGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default);

        Task<ApiResponse> SaveGeneratedQuestionsAsync(AIGeneratedQuestionsResultDto request, CancellationToken cancellationToken = default);
    }
}
