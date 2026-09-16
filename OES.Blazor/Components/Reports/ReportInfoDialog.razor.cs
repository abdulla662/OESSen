using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.InternalHelperTypes.General;

namespace OES.Blazor.Components.Reports
{
    public partial class ReportInfoDialog
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public string Title { get; set; } = string.Empty;
        [Parameter] public string Overview { get; set; } = string.Empty;
        [Parameter] public List<ReportInfoSection> Sections { get; set; } = [];

        private void Close() => MudDialog.Close();
    }
}
