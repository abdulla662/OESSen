using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Services.Interfaces.DocumentService;
using OES.Blazor.Services.Interfaces.MediaSettingService;
using OES.Helper.Dtos.Folder.Response;
using OES.Helper.Dtos.MediaSetting.Responses;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Net;
using System.Text.RegularExpressions;

namespace OES.Blazor.Components.GenericComponents.TextEditor
{
    public partial class TextEditor : IDisposable
    {
        [Inject] IBlazMediaSettingService BlazMediaSettingService { get; set; }

        [Inject] IBlazDocumentService BlazDocumentService { get; set; }

        [Inject] IJSRuntime JSRuntime { get; set; } = default!;

        [Inject] ISnackbar Snackbar { get; set; }

        [Parameter] public TextEditorParams TextEditorParams { get; set; }

        [Parameter] public RenderFragment? Footer { get; set; }

        [Parameter] public RenderFragment? ActionContent { get; set; }

        [Parameter] public EventCallback<List<string>> OnMarkedAnswersChanged { get; set; }

        private DotNetObjectReference<TextEditor>? _objRef;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _objRef = DotNetObjectReference.Create(this);

                var toolbarOptions = new
                {
                    TextEditorParams.WithFileManagerPanel,
                    TextEditorParams.WithMathChemPanel,
                    DotNetHelper = _objRef
                };

                var docLibApiBaseUri = CentralizedUrlHelper.DocLibApiBaseUrl;

                await JSRuntime.InvokeAsync<string>("initiateTextEditor",
                                                    TextEditorParams.ComponentGuid,
                                                    TextEditorParams.InitialContent ?? string.Empty,
                                                    docLibApiBaseUri,
                                                    toolbarOptions);
            }
        }


        public async Task<string> ObtainTextEditorContentAsync()
        {
            var obtainedContent = await JSRuntime.InvokeAsync<string>("getTextEditorContent", TextEditorParams.ComponentGuid);

            const string emptyContentPattern = @"<div style='direction: (ltr|rtl)'>\s*(<p>(&nbsp;|<br>|(&nbsp;\s*)+)*</p>\s*)+</div>";

            if (Regex.Match(obtainedContent, emptyContentPattern).Success)
                return string.Empty;

            if (TextEditorParams.EmbedImagesAsBase64)
            {
                obtainedContent = await JSRuntime.InvokeAsync<string>(
                    "embedAllDocLibUrlsAsBase64InHtml",
                    obtainedContent,
                    CentralizedUrlHelper.DocLibApiBaseUrl,
                    1200,
                    0.7
                );
            }

            return obtainedContent;
        }

        public async Task SetTextEditorContentAsync(string content)
        {
            await JSRuntime.InvokeVoidAsync("appendContentInTextEditor", TextEditorParams.ComponentGuid, content);
        }


        public async ValueTask DisposeJsEditorAsync()
        {
            await JSRuntime.InvokeVoidAsync("removeEditorFromTheirContainer", TextEditorParams.ComponentGuid);
        }


        private async Task ReceiveSelectedFileAsync(FolderDocumentListResponseDto insertedFile, string? url = null)
        {
            string mediaSourceUrl = !string.IsNullOrWhiteSpace(url)
                ? $"{CentralizedUrlHelper.DocLibApiBaseUrl}{url}"
                : $"{CentralizedUrlHelper.DocLibApiBaseUrl}/api/Document/DownloadStream?documentId={insertedFile.Id}";

            if (TextEditorParams.EmbedImagesAsBase64 && insertedFile.Type.ToLower().StartsWith("image/"))
            {
                var base64DataUri = await JSRuntime.InvokeAsync<string>(
                    "fetchCompressAndConvertToBase64",
                    mediaSourceUrl,
                    1200,
                    0.7
                );

                if (!string.IsNullOrEmpty(base64DataUri))
                {
                    var imgTag = $"<img src='{base64DataUri}' alt='{insertedFile.Name}' width='600' style='max-width: 600px; height: auto;' />";
                    await JSRuntime.InvokeVoidAsync("insertHtmlMediaTagIntoTextEditor", TextEditorParams.ComponentGuid, imgTag);
                    return;
                }
            }

            var mediaTag = GenerateMediaTag(insertedFile, mediaSourceUrl);

            await JSRuntime.InvokeVoidAsync("insertHtmlMediaTagIntoTextEditor", TextEditorParams.ComponentGuid, mediaTag);
        }


        private static string GenerateMediaTag(FolderDocumentListResponseDto insertedFile, string mediaUrl)
        {
            var fileType = insertedFile.Type.ToLower();

            if (fileType.StartsWith("image/"))
            {
                return $"<img src='{mediaUrl}' alt='{insertedFile.Name}' width='600' style='max-width: 600px; height: auto;' />";
            }

            if (fileType.StartsWith("video/"))
            {
                return $"<video controls width='640' height='360' src='{mediaUrl}'>Your browser does not support the video tag.</video>";
            }

            if (fileType.StartsWith("audio/"))
            {
                return $"<audio controls src='{mediaUrl}'>Your browser does not support the audio tag.</audio>";
            }

            return $"<a href='{mediaUrl}' target='_blank'>Download {insertedFile.Name}</a>";
        }


        [JSInvokable]
        public void ShowMultipleFilesError()
        {
            Snackbar.Add(Resource.UploadSingleFileOnly, Severity.Error);
        }


        [JSInvokable]
        public async Task NotifyMarkedAnswersChanged(List<string> answers)
        {
            await OnMarkedAnswersChanged.InvokeAsync(answers);
        }


        [JSInvokable]
        public async Task<bool> GetSizeLimitAsync(string type, long size)
        {
            MediaCategory category = GetCategoryFromMediaType(type);

            var mediaSetting = await BlazMediaSettingService.GetByMediaCategoryAsync(category);

            if (mediaSetting.StatusCode == HttpStatusCode.OK && mediaSetting.Data is MediaSettingResponseDto data)
            {
                long fileSizeInKB = size / 1024;
                int maxSizeInKB = data.MaxSizeInKB;

                if (fileSizeInKB > maxSizeInKB)
                {
                    Snackbar.Add(string.Format(Resource.FileSizeShouldNotExceed, maxSizeInKB), Severity.Error);
                    return false;
                }

                return true;
            }
            else
            {
                Snackbar.Add(mediaSetting.Message, Severity.Error);
                return false;
            }
        }


        [JSInvokable]
        public async Task<bool> HandleDraggableMedia(MediaFileDataJsDto jsDto)
        {
            var fileDetails = new MediaFileDataDto
            {
                Name = jsDto.Name,
                Type = jsDto.Type,
                Size = jsDto.Size,
                Content = Convert.FromBase64String(jsDto.Content)
            };

            var response = await BlazDocumentService.UploadDocumentContentAsync(fileDetails);

            if (response.StatusCode != HttpStatusCode.OK || response.Data == null)
            {
                Snackbar.Add(Resource.FailedToLoadAttachment, Severity.Error);
                return false;
            }

            FolderDocumentListResponseDto file = new FolderDocumentListResponseDto(
                Guid.Empty,
                fileDetails.Name,
                fileDetails.Type,
                fileDetails.Size,
                false
            );

            await ReceiveSelectedFileAsync(file, response.Data.FileRelativeUrl);

            return true;
        }


        public void Dispose() => _objRef?.Dispose();


        #region Helper Method

        private static MediaCategory GetCategoryFromMediaType(string mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
            {
                return MediaCategory.Image;
            }

            if (mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return MediaCategory.Video;
            }

            if (mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            {
                return MediaCategory.Audio;
            }

            return MediaCategory.Image;
        }

        #endregion
    }
}
