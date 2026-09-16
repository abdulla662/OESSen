using OES.Helper.Dtos.Paper.Responses;

namespace OES.Blazor.Services.Interfaces.PdfGeneratorService
{
    public interface IBlazePdfGeneratorService
    {
        Task<byte[]> GeneratePaperFormPdfAsync(GetPaperWithFormsDto paperWithFormsDto, bool includeDetails);
    }
}
