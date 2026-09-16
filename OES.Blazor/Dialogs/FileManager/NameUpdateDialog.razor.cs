using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace OES.Blazor.Dialogs.FileManager
{
    public partial class NameUpdateDialog : ComponentBase
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public string CurrentItemName { get; set; }


        private string _newName = string.Empty;


        protected override void OnParametersSet()
        {
            _newName = CurrentItemName;

            base.OnParametersSet();
        }


        private void UpdateFolderName()
        {
            if (string.IsNullOrWhiteSpace(_newName) || _newName == CurrentItemName)
            {
                return;
            }

            MudDialog.Close(DialogResult.Ok(_newName));

            StateHasChanged();
        }

        private void Close() => MudDialog.Cancel();
    }
}
