using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Pages.SyncStatus;

namespace OES.Blazor.Dialogs.ManualSyncOptionsDialog
{
    public partial class ManualSyncOptionsDialog : ComponentBase
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public List<CenterSyncStatus> Venues { get; set; } = [];

        private int _numberOfDays = 2;
        private bool _syncAll = true;
        private IEnumerable<string> _selectedCodes = [];

        private void Close() => MudDialog.Cancel();

        private void Confirm() => MudDialog.Close((_syncAll, _selectedCodes.ToList(), _numberOfDays));
    }
}