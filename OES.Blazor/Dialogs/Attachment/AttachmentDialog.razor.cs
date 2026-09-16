using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace OES.Blazor.Dialogs.Attachment
{
    public partial class AttachmentDialog
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public string AttachmentFileName { get; set; }

        void Close() => MudDialog.Close();
    }
}