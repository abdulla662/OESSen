using Microsoft.AspNetCore.Components.Forms;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.AIFeatures;
using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.AIFeatures
{
    public class BlazAIItemBankGenerationService : IBlazAIItemBankGenerationService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazAIItemBankGenerationService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<ApiResponse> GenerateItemBankFromFileAsync(IBrowserFile file, AIItemBankGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default)
        {
            return await _httpClientHelper.PostFileWithJsonAsync(file, configuration, "api/AIItemBankGeneration/GenerateItemBankTreeFromFile", cancellationToken: cancellationToken);
        }

        public async Task<ApiResponse> GenerateItemBankFromTextAsync(string documentText, AIItemBankGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default)
        {
            var request = new { DocumentText = documentText, Configuration = configuration };

            return await _httpClientHelper.PostAsync(request, "api/AIItemBankGeneration/GenerateItemBankTreeFromText", cancellationToken: cancellationToken);
        }

        public async Task<ApiResponse> SaveGeneratedItemBankTreeAsync(AIItemBankNodeDto treeDto, CancellationToken cancellationToken = default)
        {
            return await _httpClientHelper.PostAsync(treeDto, "api/AIItemBankGeneration/SaveGeneratedItemBankTree", cancellationToken: cancellationToken);
        }
    }
}
