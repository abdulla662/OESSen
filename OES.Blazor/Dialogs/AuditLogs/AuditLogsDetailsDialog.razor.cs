using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.AuditLogs;

namespace OES.Blazor.Dialogs.AuditLogs
{
    public partial class AuditLogsDetailsDialog
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = null!;

        [Parameter] public GetAuditLogsDto Log { get; set; } = null!;

        private void Close() => MudDialog.Close();
    }
}
