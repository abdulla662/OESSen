using Microsoft.AspNetCore.Components.Forms;
using OES.Helper.Dtos.Document.Request;
using OES.Helper.Dtos.Document.Response;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.General.NewApiResponse;

namespace OES.Blazor.Services.Interfaces.DocumentService
{
    public interface IBlazDocumentService
    {
        Task<NewApiResponse<DocumentMetadataResponseDto>> UploadDocumentAsync(IBrowserFile formFile, Guid folderId);

        Task<NewApiResponse<DocumentUrlFileResponseDto>> UploadDocumentContentAsync(MediaFileDataDto mediaFileDto);

        Task<NewApiResponse<object>> DeleteDocumentAsync(Guid documentId);

        Task<NewApiResponse<DocumentMoveRequestDto>> DocumentMoveAsync(DocumentMoveRequestDto documentMoveRequestDto, CancellationToken cancellationToken = default);

        Task<NewApiResponse<DocumentMoveRequestDto>> DocumentCopyAsync(DocumentMoveRequestDto documentMoveRequestDto, CancellationToken cancellationToken = default);

        Task<NewApiResponse<DocumentDownloadResponseDto>> DownloadDocumentAsync(Guid documentId);

        Task<NewApiResponse<DocumentUpdateResponseDto>> UpdateDocumentNameAsync(DocumentUpdateRequestDto documentUpdateRequestDto, CancellationToken cancellationToken = default);

        void SetCutDocument(Guid documentId);

        Guid? GetCutDocument();

        void ClearCutDocument();
    }
}
