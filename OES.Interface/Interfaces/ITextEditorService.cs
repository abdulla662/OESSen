using Microsoft.AspNetCore.Http;
using OES.Helper.Dtos.TextEditor;

namespace OES.Interface.Interfaces
{
    public interface ITextEditorService
    {
        Task<EditorFileUploadResponseDto> UploadImageAsync(HttpRequest request, string webRootPath);

        Task<EditorFileUploadResponseDto> UploadVideoAsync(HttpRequest request, string webRootPath);

        Task<EditorFileUploadResponseDto> UploadAudioAsync(HttpRequest request, string webRootPath);

        EditorFileDeletionResponseDto DeleteFile(string directory, string fileName, string webRootPath);
    }
}
