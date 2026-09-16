using OES.Helper.Dtos.Document.Request;
using OES.Helper.Dtos.Document.Response;

namespace OES.Interface.Interfaces
{
    public interface IDocumentExtractorService
    {
        Task<ExtractionResult> ExtractAsync(
            Stream stream,
            string fileName,
            ExtractionOptions? options = null,
            CancellationToken cancellationToken = default);
    }
}
