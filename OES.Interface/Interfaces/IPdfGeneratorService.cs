using QuestPDF.Infrastructure;

namespace OES.Interface.Interfaces
{
    public interface IPdfGeneratorService
    {
        Task<byte[]> GeneratePdfAsync(IDocument documentModel);
    }
}
