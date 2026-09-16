using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.InternalHelperTypes.General;

namespace OES.Blazor.Components.Reports
{
    public partial class ReportInfoButton
    {
        [Inject] private IDialogService DialogService { get; set; } = default!;

        [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
        [Parameter, EditorRequired] public string Overview { get; set; } = string.Empty;
        [Parameter, EditorRequired] public List<ReportInfoSection> Sections { get; set; } = [];

        private async Task OpenAsync()
        {
            var parameters = new DialogParameters
            {
                { nameof(ReportInfoDialog.Title), Title },
                { nameof(ReportInfoDialog.Overview), Overview },
                { nameof(ReportInfoDialog.Sections), Sections }
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            };

            await DialogService.ShowAsync<ReportInfoDialog>(string.Empty, parameters, options);
        }
    }
}
