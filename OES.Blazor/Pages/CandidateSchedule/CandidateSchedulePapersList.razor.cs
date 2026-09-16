using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.CandidateBatchImportHistory;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Notification;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.CandidateSchedule;

public partial class CandidateSchedulePapersList
{
    [Inject] private IBlazSchedulePaperService BlazSchedulePaperService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IBlazSessionStorageService SessionStorageService { get; set; } = default!;
    [Inject] private INotificationManager NotificationManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private List<string> _scheduleVenueCodes = [];
    private CustomTableData<SchedulePaperPaginationDto> _customTableDataOfSchedulePapers = new([], 0);
    protected int _schedulePapersListKey = 0;
    private bool _isLoadingCandidatesCount = false;

    protected override async Task OnInitializedAsync()
    {
        var selectedPaperId = await SessionStorageService.GetValue<long>("PerformViewBtnClick");

        if (selectedPaperId <= 0)
        {
            Snackbar.Add(Resource.NoScheduleSelected, Severity.Warning);
            NavigationManager.NavigateTo("/CandidateSchedeuls");
            return;
        }

        _scheduleVenueCodes = await BlazSchedulePaperService.GetSchedulePaperVenueCodesAsync(selectedPaperId);

        await NotificationManager.InitializeAsync();

        NotificationManager.OnNotificationReceived += RefreshList;
    }

    public async Task<CustomTableData<SchedulePaperPaginationDto>> GetAllSchedulePapersAsync(PaginationSearchModel paginationSearchModel)
    {
        var schedulePaperId = await SessionStorageService.GetValue<long>("PerformViewBtnClick");

        _customTableDataOfSchedulePapers = await BlazSchedulePaperService.GetAllSchedulePapersByScheduleIdAsync(paginationSearchModel, schedulePaperId);

        return _customTableDataOfSchedulePapers;
    }

    private async Task OpenCandidateBatchImportHistoryDialogAsync(SchedulePaperPaginationDto schedulePaperPaginationDto)
    {
        if (schedulePaperPaginationDto.Id <= 0)
        {
            Snackbar.Add(Resource.InvalidPaperRow, Severity.Error);
            return;
        }

        var parameters = new DialogParameters<CandidateBatchImportHistoryDialog>
        {
            { x => x.PaperId,   schedulePaperPaginationDto.Id },
            { x => x.PaperName, schedulePaperPaginationDto.PaperName }
        };

        var options = new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true };

        var dialog = await DialogService.ShowAsync<CandidateBatchImportHistoryDialog>(string.Empty, parameters, options);

        var dialogResult = await dialog.Result;

        if (!dialogResult.Canceled)
        {
            _schedulePapersListKey--;
        }
    }

    private async Task OpenDistributionDialogAsync(SchedulePaperPaginationDto schedulePaperPaginationDto)
    {
        if (schedulePaperPaginationDto.Id <= 0)
        {
            Snackbar.Add(Resource.InvalidPaperRow, Severity.Error);
            return;
        }

        var parameters = new DialogParameters<ViewDistributionDialog>
        {
            { x => x.PaperId, schedulePaperPaginationDto.Id },
            { x => x.PaperName, schedulePaperPaginationDto.PaperName },
            { x => x.TotalCandidates, schedulePaperPaginationDto.CandidatesCount }
        };

        var options = new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true };

        await DialogService.ShowAsync<ViewDistributionDialog>(string.Empty, parameters, options);
    }

    private async Task OpenDumpImportDialogAsync(SchedulePaperPaginationDto schedulePaperPaginationDto)
    {
        if (schedulePaperPaginationDto.Id <= 0)
        {
            Snackbar.Add(Resource.InvalidPaperRow, Severity.Error);
            return;
        }

        var parameters = new DialogParameters<DumpImportCandidatesDialog>
        {
            { x => x.SchedulePaperId, schedulePaperPaginationDto.Id },
            { x => x.ScheduleVenueCodes, _scheduleVenueCodes }
        };

        var options = new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true };

        var dialog = await DialogService.ShowAsync<DumpImportCandidatesDialog>(string.Empty, parameters, options);

        var dialogResult = await dialog.Result;

        if (!dialogResult.Canceled)
        {
            _isLoadingCandidatesCount = true;
        }
    }

    private async Task OpenUploadDialogAsync(SchedulePaperPaginationDto schedulePaperPaginationDto)
    {
        if (schedulePaperPaginationDto.Id <= 0)
        {
            Snackbar.Add(Resource.InvalidPerRow, Severity.Error);
            return;
        }

        var parameters = new DialogParameters<CandidatesUploadDialog>
        {
            { x => x.SchedulePaperId, schedulePaperPaginationDto.Id }
        };

        var options = new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true };

        var dialog = await DialogService.ShowAsync<CandidatesUploadDialog>(string.Empty, parameters, options);

        var dialogResult = await dialog.Result;

        if (!dialogResult.Canceled)
        {
            _isLoadingCandidatesCount = true;
        }
    }

    private void RefreshList(NotificationDto notification)
    {
        _isLoadingCandidatesCount = false;

        _schedulePapersListKey--;

        StateHasChanged();
    }
}
