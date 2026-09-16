using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OES.Blazor.Services.Implementation.DocLibHttpClientHelperService;
using OES.Blazor.Services.Interfaces.DocumentService;
using OES.Blazor.Services.Interfaces.MediaSettingService;
using OES.Helper.Dtos.Document.Request;
using OES.Helper.Dtos.Document.Response;
using OES.Helper.Dtos.MediaSetting.Responses;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.Enums;
using OES.Helper.General.NewApiResponse;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Net.Http.Headers;

namespace OES.Blazor.Services.Implementation.DocumentService
{
    public class BlazDocumentService : IBlazDocumentService
    {
        private readonly DocLibHttpClientHelper _httpClientHelper;
        private readonly ISnackbar _snackbar;
        private readonly IBlazMediaSettingService _mediaSettingService;
        private Guid? _cutDocumentId;

        public BlazDocumentService(DocLibHttpClientHelper httpClientHelper,
                                   ISnackbar snackbar,
                                   IBlazMediaSettingService mediaSettingService)
        {
            _httpClientHelper = httpClientHelper;
            _snackbar = snackbar;
            _mediaSettingService = mediaSettingService;
        }

        public async Task<NewApiResponse<object>> DeleteDocumentAsync(Guid documentId)
        {
            return await _httpClientHelper.DeleteFromJsonAsync<NewApiResponse<object>>($"Document/Delete?documentId={documentId}");

        }

        public async Task<NewApiResponse<DocumentDownloadResponseDto>> DownloadDocumentAsync(Guid documentId)
        {
            return await _httpClientHelper.GetFromJsonAsync<NewApiResponse<DocumentDownloadResponseDto>>($"Document/DownloadContent?documentId={documentId}");
        }

        public async Task<NewApiResponse<DocumentMetadataResponseDto>> UploadDocumentAsync(IBrowserFile formFile, Guid folderId)
        {
            using var form = new MultipartFormDataContent
            {
                { new StringContent(folderId.ToString()), nameof(folderId) }
            };

            var extension = Path.GetExtension(formFile.Name).ToLower();

            var category = GetMediaCategory(extension);

            if (category == null)
            {
                _snackbar.Add(Resource.UnsupportedMediaFile, Severity.Error);

                return NewApiResponse<DocumentMetadataResponseDto>.EmptyResponse();
            }

            var settingResponse = await _mediaSettingService.GetByMediaCategoryAsync(category.Value);

            long maxSize = 30;

            if (settingResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var data = (MediaSettingResponseDto)settingResponse.Data;

                if (data != null)
                {
                    maxSize = data.MaxSizeInKB;
                }
            }

            long fileSizeInKB = formFile.Size / 1024;

            if (fileSizeInKB > maxSize)
            {
                _snackbar.Add(string.Format(Resource.FileSizeShouldNotExceed, maxSize), Severity.Error);
                return NewApiResponse<DocumentMetadataResponseDto>.EmptyResponse();
            }

            using var fileContent = new StreamContent(formFile.OpenReadStream(SharedConstants.DocLibGrpcMaxMessageSize));

            if (string.IsNullOrEmpty(formFile.ContentType))
            {
                _snackbar.Add("Invalid file type. Please try again with a valid one.", Severity.Error);
                return NewApiResponse<DocumentMetadataResponseDto>.EmptyResponse();
            }

            fileContent.Headers.ContentType = new MediaTypeHeaderValue(formFile.ContentType);

            form.Add(fileContent, nameof(formFile), formFile.Name);

            var response = await _httpClientHelper.HttpClient.PostAsync("api/Document/Upload", form);

            return await _httpClientHelper.HandleResponseAsync<NewApiResponse<DocumentMetadataResponseDto>>(response);
        }

        public async Task<NewApiResponse<DocumentUrlFileResponseDto>> UploadDocumentContentAsync(MediaFileDataDto mediaFileDto)
        {
            var response = await _httpClientHelper.PostAsJsonAsync<MediaFileDataDto, NewApiResponse<DocumentUrlFileResponseDto>>(
                "Document/UploadDocumentContent",
                mediaFileDto
            );

            return response;
        }

        public async Task<NewApiResponse<DocumentUpdateResponseDto>> UpdateDocumentNameAsync(DocumentUpdateRequestDto documentUpdateRequestDto, CancellationToken cancellationToken = default)
        {
            var response = await _httpClientHelper.PutAsJsonAsync<DocumentUpdateRequestDto, NewApiResponse<DocumentUpdateResponseDto>>($"Document/UpdateDocumentName", documentUpdateRequestDto);

            return response;
        }

        public async Task<NewApiResponse<DocumentMoveRequestDto>> DocumentMoveAsync(DocumentMoveRequestDto documentMoveRequestDto, CancellationToken cancellationToken = default)
        {
            var response = await _httpClientHelper.PutAsJsonAsync<DocumentMoveRequestDto, NewApiResponse<DocumentMoveRequestDto>>($"Document/MoveDocument", documentMoveRequestDto);

            return response;
        }

        public void SetCutDocument(Guid documentId)
        {
            _cutDocumentId = documentId;
        }

        public Guid? GetCutDocument()
        {
            return _cutDocumentId;
        }

        public void ClearCutDocument()
        {
            _cutDocumentId = null;
        }

        public async Task<NewApiResponse<DocumentMoveRequestDto>> DocumentCopyAsync(DocumentMoveRequestDto documentMoveRequestDto, CancellationToken cancellationToken = default)
        {
            var response = await _httpClientHelper.PostAsJsonAsync<DocumentMoveRequestDto, NewApiResponse<DocumentMoveRequestDto>>($"Document/CopyDocument", documentMoveRequestDto);

            return response;
        }

        private static MediaCategory? GetMediaCategory(string extension)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var audioExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a" };
            var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".webm" };

            if (imageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) return MediaCategory.Image;
            if (audioExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) return MediaCategory.Audio;
            if (videoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) return MediaCategory.Video;

            return null;
        }
    }
}
