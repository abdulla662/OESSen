using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Results;

namespace OES.Blazor.Dialogs.CandidatesResult
{
    public partial class SessionDetailsDialog
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public ExamSessionModel Session { get; set; } = default!;

        private void Close() => MudDialog.Close();

        private void OnDownloadClicked() => MudDialog.Close(DialogResult.Ok(Session));
    }
}