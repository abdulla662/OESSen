using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Dialogs.CandidateBatchImportHistory;

public partial class CandidateBatchDetailsDialog : ComponentBase
{
    [Inject] private IBlazSchedulePaperService BlazSchedulePaperService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [CascadingParameter] public MudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public long VenueId { get; set; }
    [Parameter] public long PaperId { get; set; }
    [Parameter] public string VenueName { get; set; } = string.Empty;

    private List<VenueBatchDetailsDto> _batches = [];
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;

        var response = await BlazSchedulePaperService.GetVenueBatchesAsync(VenueId, PaperId);

        if (response is null)
        {
            Snackbar.Add(Resource.SomethingWentWrong, Severity.Error);
            Close();
            return;
        }

        _batches = response.Data is List<VenueBatchDetailsDto> list ? list : [];

        _isLoading = false;
        StateHasChanged();
    }

    public void Close() => MudDialog?.Close();
}