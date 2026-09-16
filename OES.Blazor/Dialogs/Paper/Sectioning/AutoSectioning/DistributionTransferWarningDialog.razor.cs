using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace OES.Blazor.Dialogs.Paper.Sectioning.AutoSectioning
{
    public partial class DistributionTransferWarningDialog
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public string ContentText { get; set; } = string.Empty;

        [Parameter] public string ButtonText { get; set; }

        [Parameter] public Color Color { get; set; } = Color.Error;

        private void Confirm() => MudDialog.Close(DialogResult.Ok(true));
    }
}
