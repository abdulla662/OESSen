using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.PdfGeneratorService;
using OES.Helper.Dtos.Paper.Responses;
using System.Net.Http.Json;

namespace OES.Blazor.Services.Implementation.PdfGeneratorService
{
    public class BlazePdfGeneratorService : IBlazePdfGeneratorService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazePdfGeneratorService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<byte[]> GeneratePaperFormPdfAsync(GetPaperWithFormsDto paperWithFormsDto, bool includeDetails)
        {
            var response = await _httpClientHelper._httpClient.PostAsJsonAsync(
                $"api/PdfGenerator/GeneratePaperFormPdf?includeDetails={includeDetails}",
                paperWithFormsDto
            );

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            return [];
        }
    }
}
