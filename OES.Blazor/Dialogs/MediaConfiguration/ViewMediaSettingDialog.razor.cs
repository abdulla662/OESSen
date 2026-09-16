using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.MediaSettingService;
using OES.Helper.Dtos.MediaSetting.Responses;
using OES.Helper.Enums;
using System.Net;

namespace OES.Blazor.Dialogs.MediaConfiguration
{
    public partial class ViewMediaSettingDialog
    {
        [Inject] private IBlazMediaSettingService MediaSettingService { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }
        [Parameter] public long MediaSettingId { get; set; }

        private MediaSettingResponseDto _mediaSetting;
        private List<string> _extensions = [];

        protected override async Task OnInitializedAsync()
        {
            var response = await MediaSettingService.GetByIdAsync(MediaSettingId);

            if (response.StatusCode == HttpStatusCode.OK && response.Data is MediaSettingResponseDto data)
            {
                _mediaSetting = data;
                _extensions = GetExtensions(_mediaSetting.MediaCategory);
            }
        }

        private static List<string> GetExtensions(MediaCategory category)
        {
            return category switch
            {
                MediaCategory.Image => [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"],
                MediaCategory.Audio => [".mp3", ".wav", ".ogg", ".m4a"],
                MediaCategory.Video => [".mp4", ".avi", ".mov", ".wmv", ".webm"],
                _ => []
            };
        }

        private void Close() => MudDialog.Close();
    }
}
