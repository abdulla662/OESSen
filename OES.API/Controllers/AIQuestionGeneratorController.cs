using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.AIQuestionGenerator.Request;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.General;
using OES.Interface.Interfaces;
using System.Text.Json;

namespace OES.API.Controllers
{
    public class AIQuestionGeneratorController : OESBaseController
    {
        private readonly IAIQuestionGenerationService _aiQuestionGenerationService;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AIQuestionGeneratorController(IAIQuestionGenerationService aiQuestionGenerationService)
        {
            _aiQuestionGenerationService = aiQuestionGenerationService;
        }

        [HttpPost("SaveGeneratedQuestions")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> SaveGeneratedQuestionsAsync(AIGeneratedQuestionsResultDto request, CancellationToken cancellationToken)
        {
            return await _aiQuestionGenerationService.SaveGeneratedQuestionsAsync(request, cancellationToken);
        }

        [HttpPost("GenerateQuestionsFromText")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GenerateQuestionsFromTextAsync([FromBody] AIQuestionGenerationTextRequestDto request, CancellationToken cancellationToken)
        {
            return await _aiQuestionGenerationService.GenerateQuestionsFromTextAsync(request.DocumentText, request.Configuration, imageCandidates: null, cancellationToken: cancellationToken);
        }

        [HttpPost("GenerateQuestionsFromFile")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GenerateQuestionsFromFileAsync(IFormFile file, [FromForm] string dto, CancellationToken cancellationToken)
        {
            var configDto = JsonSerializer.Deserialize<AIQuestionGenerationConfigurationsDto>(dto, JsonOptions)!;

            return await _aiQuestionGenerationService.GenerateQuestionsFromFileAsync(file, configDto, cancellationToken);
        }
    }
}
