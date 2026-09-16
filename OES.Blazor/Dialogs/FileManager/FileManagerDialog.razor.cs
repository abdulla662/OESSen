using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace OES.Blazor.Dialogs.FileManager
{
    public partial class FileManagerDialog
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public EventCallback<dynamic> OnFileSelected { get; set; }

        private async Task OnInternalFileSelected(dynamic file)
        {
            await OnFileSelected.InvokeAsync(file);

            MudDialog.Close(DialogResult.Ok(true));
        }

        private void Cancel() => MudDialog.Cancel();
    }
}