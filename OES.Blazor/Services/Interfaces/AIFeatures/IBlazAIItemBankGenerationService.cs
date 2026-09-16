using Microsoft.AspNetCore.Components.Forms;
using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.AIFeatures
{
    public interface IBlazAIItemBankGenerationService
    {
        Task<ApiResponse> GenerateItemBankFromFileAsync(IBrowserFile file, AIItemBankGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default);

        Task<ApiResponse> GenerateItemBankFromTextAsync(string documentText, AIItemBankGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default);

        Task<ApiResponse> SaveGeneratedItemBankTreeAsync(AIItemBankNodeDto treeDto, CancellationToken cancellationToken = default);
    }
}
