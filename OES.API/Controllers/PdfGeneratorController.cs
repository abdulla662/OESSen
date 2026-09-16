using Microsoft.AspNetCore.Mvc;
using OES.Helper.Dtos.Paper.Responses;
using OES.Interface.Interfaces;
using OES.Services.PdfGeneratorServiceDocuments;

namespace OES.API.Controllers
{
    public class PdfGeneratorController : OESBaseController
    {
        private readonly IPdfGeneratorService _pdfGeneratorService;
        private readonly IHttpClientFactory _httpClientFactory;

        public PdfGeneratorController(IPdfGeneratorService pdfGeneratorService, IHttpClientFactory httpClientFactory)
        {
            _pdfGeneratorService = pdfGeneratorService;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("GeneratePaperFormPdf")]
        public async Task<IActionResult> GeneratePaperFormPdfAsync(
            [FromBody] GetPaperWithFormsDto paperWithFormsDto,
            [FromQuery] bool includeDetails
        )
        {
            var paperFormDocument = new PaperFormDocument(paperWithFormsDto, includeDetails, _httpClientFactory);

            await paperFormDocument.PreloadImagesAsync();

            var pdfBytes = await _pdfGeneratorService.GeneratePdfAsync(paperFormDocument);

            return File(pdfBytes, "application/pdf");
        }
    }
}