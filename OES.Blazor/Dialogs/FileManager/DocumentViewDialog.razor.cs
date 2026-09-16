using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Folder.Response;
using SharedHelper.General;

namespace OES.Blazor.Dialogs.FileManager
{
    public partial class DocumentViewDialog
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

        [Parameter] public FolderDocumentListResponseDto Document { get; set; }

        private string _mediaTag = string.Empty;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                var mediaSourceUrl = $"{CentralizedUrlHelper.DocLibApiBaseUrl}/api/Document/DownloadStream?documentId={Document.Id}";

                _mediaTag = GenerateMediaTag(Document, mediaSourceUrl);

                StateHasChanged();
            }
        }

        private static string GenerateMediaTag(FolderDocumentListResponseDto file, string mediaUrl)
        {
            var fileType = file.Type.ToLower();

            return fileType switch
            {
                var t when t.StartsWith("image/")
                    => $"<img src='{mediaUrl}' alt='{file.Name}' style='max-width:100%;' />",

                var t when t.StartsWith("video/")
                    => $"<video controls style='max-width:100%;'><source src='{mediaUrl}' type='{fileType}'></video>",

                var t when t.StartsWith("audio/")
                    => $"<audio controls style='max-width:100%;'><source src='{mediaUrl}' type='{fileType}'></audio>",

                _ => $"<a href='{mediaUrl}' target='_self'>Download {file.Name}</a>"
            };
        }

        private void CloseDialog()
        {
            MudDialog.Cancel();
        }
    }
}
