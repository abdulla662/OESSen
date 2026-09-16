using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces;
using SharedHelper.Enums;

namespace OES.Blazor.Components.Common
{
    public partial class MediaAttachment : ComponentBase, IDisposable
    {
        [Inject] private IHttpClientHelper HttpClientHelper { get; set; } = null!;

        [Parameter, EditorRequired]
        public string? Url { get; set; }

        [Parameter]
        public string? Class { get; set; }

        [Parameter]
        public string? ImageStyle { get; set; } = "max-width:100%; height:auto; border-radius:4px;";

        [Parameter]
        public string? VideoStyle { get; set; } = "max-width:720px; width:100%; border-radius:4px;";

        [Parameter]
        public string? AudioStyle { get; set; } = "max-width:400px; width:100%;";

        [Parameter]
        public string? DownloadLabel { get; set; }

        [Parameter]
        public RenderFragment? FallbackContent { get; set; }

        [Parameter]
        public RenderFragment? ErrorContent { get; set; }

        [Parameter]
        public EventCallback<AttachmentType> OnTypeDetected { get; set; }

        /// <summary>
        /// If you already know the content type, pass it to skip the network request.
        /// </summary>
        [Parameter]
        public string? KnownContentType { get; set; }

        private AttachmentType _attachmentType = AttachmentType.Loading;
        private string? _contentType;
        private string? _previousUrl;
        private CancellationTokenSource? _cts;

        protected override async Task OnParametersSetAsync()
        {
            if (Url == _previousUrl)
                return;

            _previousUrl = Url;

            if (string.IsNullOrEmpty(Url))
            {
                _attachmentType = AttachmentType.Other;
                return;
            }

            if (!string.IsNullOrEmpty(KnownContentType))
            {
                _contentType = KnownContentType;
                _attachmentType = ResolveType(KnownContentType);
                await NotifyTypeDetected();
                return;
            }

            _cts?.CancelAsync();
            _cts = new CancellationTokenSource();

            _attachmentType = AttachmentType.Loading;
            _attachmentType = await DetectTypeAsync(Url, _cts.Token);
            await NotifyTypeDetected();
        }

        private async Task<AttachmentType> DetectTypeAsync(string url, CancellationToken ct)
        {
            try
            {
                // Use GET with ResponseHeadersRead - only fetches headers, not the body
                // This avoids CORS issues that HEAD requests sometimes have
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await HttpClientHelper._httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);

                if (response.Content.Headers.ContentType?.MediaType is { } mediaType)
                {
                    _contentType = mediaType;
                    return ResolveType(mediaType);
                }
            }
            catch (OperationCanceledException)
            {
                return AttachmentType.Loading;
            }
            catch
            {
                return AttachmentType.Error;
            }

            return AttachmentType.Other;
        }

        private static AttachmentType ResolveType(string contentType)
        {
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return AttachmentType.Image;
            if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                return AttachmentType.Video;
            if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
                return AttachmentType.Audio;

            return AttachmentType.Other;
        }

        private async Task NotifyTypeDetected()
        {
            if (OnTypeDetected.HasDelegate)
                await OnTypeDetected.InvokeAsync(_attachmentType);
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
