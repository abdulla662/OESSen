using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Dialogs.Question.ScreenSizeDialog
{
    public partial class ScreenSizeDialog : ComponentBase
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;
        private Dictionary<string, Breakpoint> ScreenSizes { get; set; } = new Dictionary<string, Breakpoint>
        {
            { Resource.Laptop, Breakpoint.Md },
            { Resource.Tablet, Breakpoint.Sm },
            { Resource.Phone, Breakpoint.Xs }
        };
        private Breakpoint SelectedScreenSize { get; set; } = Breakpoint.Md;

        private void Confirm() => MudDialog.Close(DialogResult.Ok(SelectedScreenSize));
        private void Cancel() => MudDialog.Cancel();
    }
}
