using Microsoft.AspNetCore.Http;
using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.General;

namespace OES.Interface.Interfaces;

public interface IAIItemBankGenerationService
{
    Task<ApiResponse> GenerateItemBankFromFileAsync(IFormFile file, AIItemBankGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default);

    Task<ApiResponse> GenerateItemBankFromTextAsync(string documentText, AIItemBankGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default);

    Task<ApiResponse> SaveGeneratedItemBankTreeAsync(AIItemBankNodeDto treeDto, CancellationToken cancellationToken = default);
}