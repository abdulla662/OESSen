using Microsoft.AspNetCore.Http;
using OES.Helper.Dtos.AIQuestionGenerator.Request;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.General;
using static OES.Helper.Dtos.Document.Response.ExtractionResult;

namespace OES.Interface.Interfaces
{
    public interface IAIQuestionGenerationService
    {
        Task<ApiResponse> GenerateQuestionsFromTextAsync(string documentText, AIQuestionGenerationConfigurationsDto configuration, List<ExtractedImage>? imageCandidates = null, CancellationToken cancellationToken = default);

        Task<ApiResponse> GenerateQuestionsFromFileAsync(IFormFile file, AIQuestionGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default);

        Task<ApiResponse> SaveGeneratedQuestionsAsync(AIGeneratedQuestionsResultDto result, CancellationToken cancellationToken = default);
    }
}
