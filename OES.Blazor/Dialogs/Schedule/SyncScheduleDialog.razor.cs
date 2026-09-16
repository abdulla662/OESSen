using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Dialogs.Schedule;

public partial class SyncScheduleDialog : ComponentBase
{
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] IBlazScheduleService BlazScheduleService { get; set; }

    [CascadingParameter] MudDialogInstance MudDialog { get; set; }
    [Parameter] public long ScheduleId { get; set; }

    private List<VenueSyncData> ScheduleVenues { get; set; } = [];
    private IEnumerable<VenueSyncData> SelectedVenues { get; set; } = new HashSet<VenueSyncData>();
    private IEnumerable<VenueSyncData> FilteredVenues =>
        string.IsNullOrWhiteSpace(_searchTerm)
            ? ScheduleVenues
            : ScheduleVenues.Where(v => v.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase));
    private bool IsAllSelected =>
        ScheduleVenues.Count > 0 && ScheduleVenues.All(v => SelectedVenues.Contains(v));

    private bool _isOkButtonHit;

    private string _searchTerm = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var schedule = await BlazScheduleService.GetScheduleByIdAsync(ScheduleId);

        ScheduleVenues = schedule.ExamVenueNames;
    }

    void Cancel() => MudDialog.Cancel();

    private async Task BulkSync()
    {
        _isOkButtonHit = true;
        StateHasChanged();

        if (SelectedVenues.Any())
        {
            MudDialog.Close(DialogResult.Ok((SyncTypes.Bulk, SelectedVenues.Select(x => x.Id).ToList())));
        }
        else
        {
            Snackbar.Add(Resource.SelectOneOrMoreVenuesForTheSchedule, Severity.Warning);
            _isOkButtonHit = false;
            StateHasChanged();
        }
    }

    private void SyncAll()
    {
        MudDialog.Close(DialogResult.Ok((SyncTypes.Bulk, ScheduleVenues.ConvertAll(x => x.Id))));
    }

    private void ToggleVenue(VenueSyncData venue, bool selected)
    {
        var set = new HashSet<VenueSyncData>(SelectedVenues);
        if (selected) set.Add(venue);
        else set.Remove(venue);
        SelectedVenues = set;
    }

    private void ToggleSelectAll(bool selectAll)
    {
        SelectedVenues = selectAll ? [.. ScheduleVenues] : new HashSet<VenueSyncData>();
    }
}