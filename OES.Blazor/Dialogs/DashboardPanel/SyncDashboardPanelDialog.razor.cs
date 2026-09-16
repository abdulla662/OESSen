using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Helper.Dtos.Sync;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using SharedHelper.General;
using System.Net;

namespace OES.Blazor.Dialogs.DashboardPanel
{
    public partial class SyncDashboardPanelDialog : ComponentBase, IAsyncDisposable
    {
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazAuthService BlazAuthService { get; set; }
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISyncStatusService SyncStatusService { get; set; }
        [Inject] private ISyncToExamServer SyncToExamServer { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [CascadingParameter] private MudDialogInstance DialogInstance { get; set; }
        [Parameter] public List<SyncJobStatusDto> InitialJobs { get; set; } = [];
        [Parameter] public EventCallback<IEnumerable<long>> OnRetryFailedJobs { get; set; }
        [Parameter] public EventCallback<long> OnRetrySingleJob { get; set; }
        [Parameter] public EventCallback OnMinimizeClicked { get; set; }
        [Parameter] public bool IsPreparationMode { get; set; } = false;
        [Parameter] public long PreparationScheduleId { get; set; }
        [Parameter] public string PreparationScheduleName { get; set; }

        private IEnumerable<SyncJobStatusDto> FailedJobs => SyncJobs.Where(j => j.Status == SyncJobStatus.Failed);
        private IEnumerable<SyncJobStatusDto> FilteredSyncJobs => _showOnlyFailed ? FailedJobs : SyncJobs;
        private IEnumerable<SyncJobStatusDto> SortedFilteredJobs => FilteredSyncJobs.OrderBy(j => j.VenueName);
        private bool HasPendingJobs => SyncJobs.Any(j => j.Status == SyncJobStatus.Pending);
        private bool IsOperationComplete => SyncJobs.Count > 0 && SyncJobs.All(j => j.Status == SyncJobStatus.Success || j.Status == SyncJobStatus.Failed);
        private int PendingCount => SyncJobs.Count(j => j.Status == SyncJobStatus.Pending);
        private int InProgressCount => SyncJobs.Count(j => j.Status == SyncJobStatus.InProgress);
        private int SuccessCount => SyncJobs.Count(j => j.Status == SyncJobStatus.Success);
        private int FailedCount => SyncJobs.Count(j => j.Status == SyncJobStatus.Failed);
        private int CompletedCount => SuccessCount + FailedCount;
        private int TotalJobs => SyncJobs.Count;
        private double ProgressPercentage => TotalJobs > 0 ? ((double)CompletedCount / TotalJobs * 100) : 0;

        private HubConnection _hubConnection;
        private List<SyncJobStatusDto> SyncJobs = [];
        private bool isRetrying = false;
        private long _scheduleId;
        private Guid _batchId;
        private bool _showOnlyFailed = false;
        private bool _isPreparingData = false;
        private bool _isDisposed = false;
        private CancellationTokenSource _cts = new();
        private bool IsLoadingHistory = false;
        private List<SyncJobStatusDto> SyncHistoryJobs = [];

        protected override async Task OnInitializedAsync()
        {
            if (IsPreparationMode)
            {
                _isPreparingData = true;

                _scheduleId = PreparationScheduleId;

                SyncStatusService.StatusChanged += OnJobsArrived;

                return;
            }

            if (InitialJobs.Count == 0)
            {
                SyncJobs = SyncStatusService.GetAllJobs();

                if (SyncJobs.Count == 0)
                {
                    return;
                }
            }
            else
            {
                SyncJobs = InitialJobs;
            }

            var firstJob = SyncJobs.FirstOrDefault();

            if (firstJob == null)
            {
                return;
            }

            _scheduleId = firstJob.ScheduleId;
            _batchId = firstJob.BatchId;

            if (_scheduleId == 0)
            {
                return;
            }

            if (_batchId == Guid.Empty)
            {
                return;
            }

            await InitializeSignalR();
        }

        private void OnJobsArrived()
        {
            if (_isDisposed) return;

            var jobs = SyncStatusService.GetAllJobs();

            if (jobs.Count > 0)
            {
                _isPreparingData = false;

                SyncJobs = jobs;

                var firstJob = SyncJobs.FirstOrDefault();

                if (firstJob != null)
                {
                    _scheduleId = firstJob.ScheduleId;

                    _batchId = firstJob.BatchId;
                }

                _ = InvokeAsync(async () =>
                {
                    if (_isDisposed) return;

                    await InitializeSignalR();

                    if (!_isDisposed) StateHasChanged();
                });
            }
        }

        private async Task InitializeSignalR()
        {
            if (_isDisposed) return;

            if (_hubConnection != null)
            {
                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                    await RefreshDataAsync();
                }

                return;
            }

            var hubUrl = $"{CentralizedUrlHelper.OesApiBaseUrl.TrimEnd('/')}/syncDashboardHub";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => options.AccessTokenProvider = () => BlazAuthService.GetDecryptedTokenFromLocalStorageAsync())
                .WithAutomaticReconnect([
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                ])
                .Build();

            _hubConnection.Reconnecting += async error => await InvokeAsync(() => StateHasChanged());

            _hubConnection.Reconnected += async connectionId =>
            {
                await InvokeAsync(() =>
                {
                    Snackbar.Add(Resource.Reconnectedtosyncupdates, Severity.Success);
                    StateHasChanged();
                });

                await RefreshDataAsync();
            };

            _hubConnection.Closed += async _ =>
            {
                if (_isDisposed) return;

                await InvokeAsync(() => StateHasChanged());

                await Task.Delay(new Random().Next(0, 5) * 1000);

                if (_isDisposed) return;

                try
                {
                    await _hubConnection.StartAsync();

                    await RefreshDataAsync();
                }
                catch (Exception ex)
                {
                    if (!_isDisposed) Snackbar.Add(ex.Message, Severity.Warning);
                }
            };

            _hubConnection.On<long, int, string, DateTime?>(SignalRCommonConstant.UpdateJobStatus,
                async (jobId, status, errorMessage, completedAt) =>
                {
                    if (_isDisposed) return;

                    await InvokeAsync(() =>
                    {
                        if (_isDisposed) return;

                        var existingJob = SyncJobs.FirstOrDefault(j => j.Id == jobId);

                        if (existingJob != null)
                        {
                            existingJob.Status = (SyncJobStatus)status;
                            existingJob.CompletedAt = completedAt;
                            existingJob.ErrorMessage = errorMessage;

                            SyncStatusService.UpdateJob(jobId, (SyncJobStatus)status, errorMessage, completedAt);

                            StateHasChanged();
                        }
                    });
                }
            );

            await _hubConnection.StartAsync();

            await RefreshDataAsync();

            await InvokeAsync(() => StateHasChanged());
        }

        private async Task RefreshDataAsync()
        {
            if (_isDisposed || _hubConnection == null) return;

            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync(SignalRCommonConstant.JoinScheduleGroup, _scheduleId);
                await _hubConnection.InvokeAsync(SignalRCommonConstant.RequestCurrentStatus, _scheduleId, _batchId);
            }
        }

        private void OnFilterChanged(bool isChecked)
        {
            _showOnlyFailed = isChecked;
            StateHasChanged();
        }

        public void SetRetryState(bool isRetryingState)
        {
            isRetrying = isRetryingState;
            StateHasChanged();
        }

        private async Task MinimizePanel()
        {
            await OnMinimizeClicked.InvokeAsync();
            DialogInstance.Close();
        }

        private async Task ShowErrorDetails(SyncJobStatusDto job)
        {
            await DialogService.ShowMessageBox($" {job.VenueName}", job.ErrorMessage, yesText: Resource.Ok);
        }

        private async Task OnTabChanged(int tabIndex)
        {
            if (tabIndex == 1)
            {
                await LoadSyncHistoryAsync();
            }
        }

        private async Task LoadSyncHistoryAsync()
        {
            IsLoadingHistory = true;
            StateHasChanged();

            var fromDate = DateTime.UtcNow.AddDays(-2);

            SyncHistoryJobs = await SyncToExamServer.GetSyncHistoryAsync(fromDate);

            IsLoadingHistory = false;
            StateHasChanged();
        }

        private async Task CancelPendingJobAsync(long jobId)
        {
            var response = await SyncToExamServer.CancelPendingJobAsync(jobId);
            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(Resource.SavedSuccessfully, Severity.Success);

                var existingJob = SyncJobs.FirstOrDefault(j => j.Id == jobId);
                if (existingJob != null)
                {
                    existingJob.Status = SyncJobStatus.Failed;
                    existingJob.ErrorMessage = "Cancelled manually by user";
                }

                var historyJob = SyncHistoryJobs.FirstOrDefault(j => j.Id == jobId);
                if (historyJob != null)
                {
                    historyJob.Status = SyncJobStatus.Failed;
                    historyJob.ErrorMessage = "Cancelled manually by user";
                }

                StateHasChanged();
            }
            else
            {
                Snackbar.Add(Resource.FailedToSaveData, Severity.Error);
            }
        }

        private async Task DownloadPayloadAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Snackbar.Add(Resource.InvalidDataFormat, Severity.Error);
                return;
            }

            var bytes = await SyncToExamServer.GetPayloadBytesAsync(fileName);

            if (bytes != null)
            {
                await JSRuntime.InvokeVoidAsync("downloadFileFromBytes", bytes, Path.GetFileName(fileName));
            }
            else
            {
                Snackbar.Add(Resource.Error, Severity.Error);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed) return;

            _isDisposed = true;

            await _cts.CancelAsync();

            SyncStatusService.StatusChanged -= OnJobsArrived;

            if (_hubConnection != null)
            {
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync(SignalRCommonConstant.LeaveScheduleGroup, _scheduleId);
                    await _hubConnection.StopAsync();
                }

                await _hubConnection.DisposeAsync();
            }

            _cts.Dispose();
        }
    }
}
