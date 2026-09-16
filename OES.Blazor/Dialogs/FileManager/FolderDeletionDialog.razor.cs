using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace OES.Blazor.Dialogs.FileManager
{
    public partial class FolderDeletionDialog
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;

        private void Cancel() => MudDialog.Cancel();

        private void Confirm() => MudDialog.Close(DialogResult.Ok(true));
    }
}
