using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Schedule;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Implementation;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.Sync;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Pages.Schedule.Main.SchedulesList
{
    public partial class ScheduleList : IDisposable
    {
        [Inject] private IBlazScheduleService BlazScheduleService { get; set; }

        [Inject] private ISyncToExamServer SyncToExamServer { get; set; }

        [Inject] private IDialogService DialogService { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; }

        [Inject] private NavigationManager NavigationManager { get; set; }

        [Inject] private IBlazAuthService AuthService { get; set; }

        [Inject] private SyncDashboardState SyncDashboardState { get; set; }

        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] public GlobalUserContext GlobalUserContext { get; set; } = default!;

        private int scheduleKey;
        private long? syncingScheduleId = null;
        private string _selectedLocation = string.Empty;
        private string _selectedStatus = string.Empty;
        private string _selectedSyncStatus = string.Empty;
        private readonly List<string> _locations = Enum.GetNames<ScheduleLocation>().ToList();
        private readonly List<string> _statuses = Enum.GetNames<PublishingStatus>().ToList();
        private readonly List<string> _syncStatuses = Enum.GetNames<SyncingStatus>().ToList();
        private PeriodicTimer? _countdownTimer;
        private CancellationTokenSource? _timerCts;
        private string timeRemaining = "00:00";

        public ScheduleFilterPaginationModel ScheduleFilterPaginationModel { get; set; } = new ScheduleFilterPaginationModel();

        public string SelectedSyncStatus
        {
            get => _selectedSyncStatus;
            set
            {
                if (_selectedSyncStatus != value)
                {
                    _selectedSyncStatus = value;

                    if (!string.IsNullOrEmpty(_selectedSyncStatus))
                    {
                        ScheduleFilterPaginationModel.SelectedSyncStatus = Enum.Parse<SyncingStatus>(_selectedSyncStatus);
                    }
                    else
                    {
                        ScheduleFilterPaginationModel.SelectedSyncStatus = null;
                    }

                    scheduleKey += 1;
                }
            }
        }

        public string SelectedLocation
        {
            get => _selectedLocation;

            set
            {
                if (_selectedLocation != value)
                {
                    _selectedLocation = value;

                    if (!string.IsNullOrEmpty(_selectedLocation))
                    {
                        ScheduleFilterPaginationModel.SelectedLocation = Enum.Parse<ScheduleLocation>(_selectedLocation);
                    }
                    else
                    {
                        ScheduleFilterPaginationModel.SelectedLocation = new ScheduleLocation();
                    }

                    scheduleKey += 1;
                }
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;

            set
            {
                if (_selectedStatus != value)
                {
                    _selectedStatus = value;

                    if (!string.IsNullOrEmpty(_selectedStatus))
                    {
                        ScheduleFilterPaginationModel.SelectedStatus = Enum.Parse<PublishingStatus>(_selectedStatus);
                    }
                    else
                    {
                        ScheduleFilterPaginationModel.SelectedStatus = new PublishingStatus();
                    }

                    scheduleKey += 1;
                }
            }
        }

        protected override void OnInitialized()
        {
            SyncDashboardState.OnGlobalSyncStateChanged += HandleGlobalSyncStateChanged;

            if (SyncDashboardState.IsGlobalSyncActive)
            {
                if (SyncDashboardState.GlobalSyncUnlockTime.HasValue && SyncDashboardState.GlobalSyncUnlockTime.Value <= DateTime.UtcNow)
                {
                    SyncDashboardState.SetGlobalSyncActive(false, null);
                }
                else
                {
                    StartCountdown();
                }
            }
        }

        private void HandleGlobalSyncStateChanged()
        {
            if (SyncDashboardState.IsGlobalSyncActive)
                StartCountdown();
            else
                StopCountdown();

            InvokeAsync(StateHasChanged);
        }

        private async Task DeleteScheduleAsync(long scheduleId)
        {
            var authorized = await AuthService.IsCurrentUserAuthorizedAsync(
                scheduleId,
                OesTemplateRoleConstants.ScheduleDeleter,
                BlazScheduleService.GetScheduleGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToDeleteSchedule, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.ConfirmDeleteScheduleMessage },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<GenericDialog>(Resource.DeleteSchedule, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazScheduleService.DeleteScheduleAsync(scheduleId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    scheduleKey++;
                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }

        private async Task CopyScheduleDeeply(ScheduleMetadataPaginationDto schedule)
        {
            if (schedule.Id > 0)
            {
                var response = await BlazScheduleService.CopyScheduleAsync(schedule.Id);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(response.Message, Severity.Success);

                    scheduleKey++;
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }
            }
        }

        private async Task OpenCopyScheduleDeeplyDialogAsync(ScheduleMetadataPaginationDto schedule)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.Alert },
                { x => x.Content, Resource.ConfirmCopyScheduleMessage },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Copy },
                { x => x.SubmitButtonColor, Color.Primary },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.ContentCopy }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Small
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await CopyScheduleDeeply(schedule);
            }
        }

        private async Task SyncSchedule(ScheduleMetadataPaginationDto schedule)
        {
            var authorized = await AuthService.IsCurrentUserAuthorizedAsync(
                schedule.Id,
                OesTemplateRoleConstants.ScheduleSync,
                BlazScheduleService.GetScheduleGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var validationResult = await SyncToExamServer.ValidateScheduleForSyncAsync(schedule.Id);

            if (validationResult.StatusCode != HttpStatusCode.OK)
            {
                Snackbar.Add(validationResult.Message, Severity.Error);
                return;
            }

            syncingScheduleId = schedule.Id;

            var parameters = new DialogParameters<SyncScheduleDialog>
            {
                { x => x.ScheduleId, schedule.Id }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true, BackdropClick = false };

            var dialog = await DialogService.ShowAsync<SyncScheduleDialog>(Resource.SyncScheduleOptions, parameters, options);

            var result = await dialog.Result;

            if (result.Canceled)
            {
                syncingScheduleId = null;
                StateHasChanged();
                return;
            }

            var (syncType, venues) = ((SyncTypes, List<long>))result.Data;

            if (SyncDashboardState.IsGlobalSyncActive && SyncDashboardState.LockedOrganizationId == GlobalUserContext.CurrentOrganizationId)
            {
                Snackbar.Add(Resource.AnotherScheduleSyncing, Severity.Warning);
                return;
            }

            SyncDashboardState.StartPreparation(schedule.Id, schedule.Name);

            try
            {
                var response = await SyncToExamServer.BulkSyncToExamServerAsync(schedule.Id, venues);

                if (response.StatusCode == HttpStatusCode.Accepted)
                {
                    var jsonElement = (JsonElement)response.Data;

                    var initialJobs = jsonElement.Deserialize<List<SyncJobStatusDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    SyncDashboardState.StartSync(initialJobs);

                    Snackbar.Add(response.Message, Severity.Success);

                    scheduleKey++;
                }
                else
                {
                    SyncDashboardState.StopPreparation();

                    Snackbar.Add(response.Message, Severity.Info, config =>
                    {
                        config.RequireInteraction = true;
                        config.ShowCloseIcon = true;
                        config.VisibleStateDuration = int.MaxValue;
                        config.HideTransitionDuration = 500;
                        config.ShowTransitionDuration = 300;
                        config.SnackbarVariant = Variant.Filled;
                        config.ActionColor = Color.Default;
                    });
                }
            }
            catch (Exception ex)
            {
                SyncDashboardState.StopPreparation();

                Snackbar.Add(ex.Message, Severity.Error);
            }
            finally
            {
                syncingScheduleId = null;
                StateHasChanged();
            }
        }

        private async Task ViewScheduleAsync(long scheduleId)
        {
            var authorized = await AuthService.IsCurrentUserAuthorizedAsync(
                scheduleId,
                OesTemplateRoleConstants.ScheduleViewer,
                BlazScheduleService.GetScheduleGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToViewSchedule, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(scheduleId, MiscConstants.PerformViewBtnClick);

            var parameters = new DialogParameters<ScheduleViewDialog>();

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

            var dialog = await DialogService.ShowAsync<ScheduleViewDialog>(Resource.ScheduleDetails, parameters, options);

            await dialog.Result;
        }

        private async Task NavigateToEditAsync(long scheduleId)
        {
            var authorized = await AuthService.IsCurrentUserAuthorizedAsync(
                scheduleId,
                OesTemplateRoleConstants.ScheduleEditor,
                BlazScheduleService.GetScheduleGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToEditSchedule, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(scheduleId, MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/CreateOrUpdateSchedule");
        }

        private void ResetFilters()
        {
            if (!string.IsNullOrEmpty(_selectedLocation) || !string.IsNullOrEmpty(_selectedStatus) || !string.IsNullOrEmpty(_selectedSyncStatus))
            {
                _selectedLocation = string.Empty;
                _selectedStatus = string.Empty;
                _selectedSyncStatus = string.Empty;

                ScheduleFilterPaginationModel.SelectedLocation = new ScheduleLocation();
                ScheduleFilterPaginationModel.SelectedStatus = new PublishingStatus();
                ScheduleFilterPaginationModel.SelectedSyncStatus = null;

                scheduleKey++;
            }
        }

        private Task<IEnumerable<string>> SearchLocations(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_locations.AsEnumerable());

            return Task.FromResult(_locations.Where(x => x.ToLocalizedString<ScheduleLocation>().Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        private Task<IEnumerable<string>> SearchStatuses(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_statuses.AsEnumerable());

            return Task.FromResult(_statuses.Where(x => x.ToLocalizedString<PublishingStatus>().Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        private Task<IEnumerable<string>> SearchSyncStatuses(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_syncStatuses.AsEnumerable());

            return Task.FromResult(_syncStatuses.Where(x => x.ToLocalizedString<SyncingStatus>().Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        private void StartCountdown()
        {
            StopCountdown();

            _timerCts = new CancellationTokenSource();

            _countdownTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            _ = RunCountdownLoopAsync(_timerCts.Token);
        }

        private void StopCountdown()
        {
            _timerCts?.Cancel();
            _timerCts?.Dispose();
            _countdownTimer?.Dispose();
            _timerCts = null;
            _countdownTimer = null;
        }

        private async Task RunCountdownLoopAsync(CancellationToken ct)
        {
            try
            {
                while (await _countdownTimer!.WaitForNextTickAsync(ct))
                {
                    if (!SyncDashboardState.GlobalSyncUnlockTime.HasValue) break;

                    var remaining = SyncDashboardState.GlobalSyncUnlockTime.Value - DateTime.UtcNow;

                    if (remaining.TotalSeconds > 0)
                    {
                        timeRemaining = $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";
                    }
                    else
                    {
                        timeRemaining = "00:00";
                        StopCountdown();
                        await InvokeAsync(() => SyncDashboardState.SetGlobalSyncActive(false, null));
                        break;
                    }

                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (OperationCanceledException) { /* Normal Cancellation */ }
        }

        public void Dispose()
        {
            SyncDashboardState.OnGlobalSyncStateChanged -= HandleGlobalSyncStateChanged;
            StopCountdown();
        }
    }
}