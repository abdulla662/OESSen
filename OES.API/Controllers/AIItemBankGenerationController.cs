using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.General;
using OES.Interface.Interfaces;
using System.Text.Json;

namespace OES.API.Controllers
{
    public class AIItemBankGenerationController : OESBaseController
    {
        private readonly IAIItemBankGenerationService _aiItemBankGenerationService;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AIItemBankGenerationController(IAIItemBankGenerationService aiItemBankGenerationService)
        {
            _aiItemBankGenerationService = aiItemBankGenerationService;
        }

        [HttpPost("GenerateItemBankTreeFromFile")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GenerateItemBankFromFileAsync(IFormFile file, [FromForm] string dto, CancellationToken cancellationToken = default)
        {
            var configuration = JsonSerializer.Deserialize<AIItemBankGenerationConfigurationsDto>(dto, JsonOptions);
            return await _aiItemBankGenerationService.GenerateItemBankFromFileAsync(file, configuration, cancellationToken);
        }

        [HttpPost("GenerateItemBankTreeFromText")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GenerateItemBankFromTextAsync(string documentText, AIItemBankGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default)
        {
            return await _aiItemBankGenerationService.GenerateItemBankFromTextAsync(documentText, configuration, cancellationToken);
        }

        [HttpPost("SaveGeneratedItemBankTree")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> SaveGeneratedItemBankTreeAsync(AIItemBankNodeDto treeDto, CancellationToken cancellationToken = default)
        {
            return await _aiItemBankGenerationService.SaveGeneratedItemBankTreeAsync(treeDto, cancellationToken);
        }
    }
}
