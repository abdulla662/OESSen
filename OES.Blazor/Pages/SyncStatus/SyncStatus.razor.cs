using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.ManualSyncOptionsDialog;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.CandidatesResult;
using OES.Helper.Dtos.CBTCandidates;
using OES.Helper.Dtos.Results;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using SharedHelper.General;
using System.Text.Json;

namespace OES.Blazor.Pages.SyncStatus
{
    public partial class SyncStatus
    {
        [Inject] private IDialogService DialogService { get; set; } = null!;
        [Inject] private ISyncToExamServer SyncToExamServer { get; set; } = null!;
        [Inject] private IBlazSyncToEvaluation BlazSyncToEvaluation { get; set; } = null!;
        [Inject] private IBlazAuthService AuthService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private IBlazCandidatesResultService CandidatesResultService { get; set; } = null!;
        [Inject] private IBlazCandidatesResultService BlazCandidatesResult { get; set; } = null!;
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;

        private Timer _refreshTimer;
        private bool _isPolling = false;
        private bool _isSyncRunning = false;

        private List<CBTSyncStatusResponseDto> _cbtSyncStatus = [];
        private List<SyncStatusOESToCESReportDto> _syncStatusOESToCESReportList = [];
        private SyncStatusOESToCESReportDto _syncStatusOESToCESReport = new();
        private List<SyncStatusCESToOESReportDto> _syncStatusCESToOESReportList = [];
        private SyncStatusCESToOESReportDto _syncStatusCESToOESReport = new();

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Filter properties
        private DateTime? _selectedDate = DateTime.Today;
        private SyncRunType _selectedRunType = SyncRunType.Daily;
        private IEnumerable<string> _selectedSeriesCodes = new List<string>();
        private string? _selectedCenter;
        private IReadOnlyCollection<SyncJobStatus> _selectedStatusFilters = new List<SyncJobStatus>();
        private ICollection<SyncJobStatus> _tableStatusFilters = new HashSet<SyncJobStatus>();

        // Exam series codes for filter
        private readonly List<string> _examSeriesCodes = new()
        {
            "CS-2025-A", "CS-2025-B", "CS-2025-C", "CS-2025-D",
            "MATHS-2025-1", "MATHS-2025-2", "ENG-2025-A"
        };

        // KPI Card Data - Forward Sync (Central to Centers)
        private string _syncStatusToday = "Completed";
        private string _lastRunTimestamp = "2025-12-05 08:15:32";
        private string _totalRunDuration = "12m 34s";
        private int _syncProgress = 100;

        // KPI Card Data - Reverse Sync (Centers to Central)
        private string _reverseSyncStatusToday = "Running";
        private string _reverseLastRunTimestamp = "2025-12-05 09:30:15";
        private string _reverseTotalRunDuration = "8m 12s";
        private int _reverseSyncProgress = 87;

        // CBT → Central KPI Card Data (loaded from database)
        private int _totalExamineesPulled = 0;
        private int _importedToCentral = 0;
        private int _assignedToPaperCount = 0;
        private int _notAssignedToPaperCount = 0;
        private int _rejectedRecords = 0;
        private double _cbtImportProgress = 0;
        private string _cbtLastRunTimestamp = "-";

        private int _totalDistributed = 15250;
        private double _coveragePercentage = 99.2;

        private int _centersSynced = 72;
        private int _totalCenters = 75;
        private int _centersWithIssues = 3;
        private int _greenCenters = 68;
        private int _amberCenters = 4;
        private int _redCenters = 3;

        private int _maxLatency = 2340;
        private int _avgLatency = 450;
        private int _minLatency = 120;

        private int _failedJobs = 3;
        private int _examineesFailed = 35;
        private string _commonErrorCode = "ERR_TIMEOUT_503";

        // Pipeline Step Data
        private Color _step1Color = Color.Default;
        private string _step1Status = "No Data";
        private int _step1ApiCalls = 0;
        private int _step1Examinees = 0;
        private int _step1AvgTime = 0;

        private Color _step2Color = Color.Success;
        private string _step2Status = "Completed";
        private int _step2Valid = 15380;
        private int _step2Rejected = 40;
        private int _step2Centers = 75;

        private Color _step3Color = Color.Warning;
        private string _step3Status = "Partial";
        private int _step3Distributed = 15250;
        private int _step3Success = 68;
        private int _step3Partial = 4;
        private int _step3Failed = 3;
        private double _step3Progress = 90.7;

        private Color _step4Color = Color.Info;
        private string _step4Status = "Running";
        private int _step4Acked = 65;
        private int _step4Pending = 7;
        private int _step4Timeout = 3;
        private double _step4Progress = 86.7;

        // Reverse Pipeline Step Data (Exam Servers → Central)
        private Color _reverseStep1Color = Color.Default;
        private string _reverseStep1Status = "Loading...";
        private int _reverseStep1Completed = 0;
        private int _reverseStep1ActiveCenters = 0;
        private double _reverseStep1CompletionRate = 0;

        private Color _reverseStep2Color = Color.Success;
        private string _reverseStep2Status = "Processing";
        private int _reverseStep2Packaged = 14100;
        private int _reverseStep2Pending = 320;
        private int _reverseStep2Failed = 100;
        private double _reverseStep2Progress = 97.1;

        private Color _reverseStep3Color = Color.Info;
        private string _reverseStep3Status = "Uploading";
        private int _reverseStep3Uploaded = 13800;
        private int _reverseStep3InProgress = 300;
        private int _reverseStep3UploadFailed = 100;
        private double _reverseStep3Progress = 95.1;

        private Color _reverseStep4Color = Color.Success;
        private string _reverseStep4Status = "Verifying";
        private int _reverseStep4Verified = 13500;
        private int _reverseStep4Pending = 300;
        private int _reverseStep4Failed = 100;
        private double _reverseStep4Progress = 93.0;

        // Per-Center Status Table
        private List<CenterSyncStatus> _centerStatuses = new();
        private CenterSyncStatus? _selectedCenterStatus;

        // Charts Data
        private List<ChartSeries> _volumeTrendSeries = new();
        private string[] _volumeTrendLabels = Array.Empty<string>();

        private double[] _latencyData = Array.Empty<double>();
        private string[] _latencyLabels = Array.Empty<string>();

        private List<ChartSeries> _regionVolumeSeries = new();
        private string[] _regionLabels = Array.Empty<string>();

        private ChartOptions _chartOptions = new()
        {
            YAxisTicks = 5000,
            InterpolationOption = InterpolationOption.NaturalSpline
        };

        // Error Tables Data
        private List<ErrorSummary> _errorSummaries = new();
        private List<FailedJob> _failedJobsList = new();

        // Active Alerts
        private List<AlertInfo> _activeAlerts = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadCBTSyncStatsAsync();

            await LoadExamCompletionStatsAsync();

            await LoadCentersSyncStatusAsync();

            await SyncStatusOESToCESReportAsync();

            await SyncStatusCESToOESReportAsync();

            try
            {
                var syncState = await SyncToExamServer.IsSyncRunningAsync();
                if (syncState.Data is JsonElement j)
                    _isSyncRunning = j.GetBoolean();
            }
            catch { }

            LoadMockData();
        }

        private async Task LoadCBTSyncStatsAsync()
        {
            var response = await SyncToExamServer.GetCBTSyncStatusAsync();

            if (response.CustomCodeStatus == CustomCodeStatus.Success && response.Data != null)
            {
                // Deserialize the data from JsonElement
                if (response.Data is JsonElement jsonElement)
                {
                    _cbtSyncStatus = JsonSerializer.Deserialize<List<CBTSyncStatusResponseDto>>(jsonElement.GetRawText(), _jsonOptions) ?? new();
                }
                else
                {
                    _cbtSyncStatus = response.Data as List<CBTSyncStatusResponseDto> ?? new();
                }

                if (_cbtSyncStatus.Count > 0)
                {
                    // Calculate totals from all venues
                    _totalExamineesPulled = _cbtSyncStatus.Sum(s => s.TotalCandidates);
                    _importedToCentral = _cbtSyncStatus.Sum(s => s.ImportedCount);
                    _assignedToPaperCount = _cbtSyncStatus.Sum(s => s.AssignedToPaperCount);
                    _notAssignedToPaperCount = _cbtSyncStatus.Sum(s => s.NotAssignedToPaperCount);
                    _rejectedRecords = _totalExamineesPulled - _importedToCentral;

                    // Get the latest run time
                    var lastRun = _cbtSyncStatus.Where(s => s.LastRunTime.HasValue)
                                                .OrderByDescending(s => s.LastRunTime)
                                                .FirstOrDefault();
                    if (lastRun?.LastRunTime != null)
                    {
                        _cbtLastRunTimestamp = lastRun.LastRunTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    // Calculate import progress
                    _cbtImportProgress = _totalExamineesPulled > 0
                        ? Math.Round((double)_importedToCentral / _totalExamineesPulled * 100, 1)
                        : 0;

                    // Update CBT Pull (Step 1) card from view data
                    _step1ApiCalls = _cbtSyncStatus.Select(s => s.VenueCode).Distinct().Count();
                    _step1Examinees = _totalExamineesPulled;
                    _step1AvgTime = (int)(_cbtSyncStatus.Count > 0 ? _cbtSyncStatus.Average(s => s.AvgTimeMs) : 0);

                    // Dynamic status based on imported vs total
                    if (_totalExamineesPulled == 0)
                    {
                        _step1Status = "No Data";
                        _step1Color = Color.Default;
                    }
                    else if (_importedToCentral == _totalExamineesPulled)
                    {
                        _step1Status = "Completed";
                        _step1Color = Color.Success;
                    }
                    else if (_importedToCentral > 0)
                    {
                        _step1Status = "Partial";
                        _step1Color = Color.Warning;
                    }
                    else
                    {
                        _step1Status = "Pending";
                        _step1Color = Color.Info;
                    }
                }
            }
        }

        private async Task LoadExamCompletionStatsAsync()
        {
            var response = await CandidatesResultService.GetAllAttendanceReportsAsync();

            if (response.CustomCodeStatus != CustomCodeStatus.Success || response.Data == null)
                return;

            var reports = response.Data is JsonElement json
                ? JsonSerializer.Deserialize<List<PaperFormAttendanceReportDto>>(json.GetRawText(), _jsonOptions) ?? []
                : response.Data as List<PaperFormAttendanceReportDto> ?? [];

            if (reports.Count == 0)
                return;

            var totalAttended = reports.Sum(r => r.Attended);

            _reverseStep1Completed = reports.Sum(r => r.TakenSession);
            _reverseStep1ActiveCenters = reports.Select(r => r.VenueCode).Where(v => !string.IsNullOrEmpty(v)).Distinct().Count();
            _reverseStep1CompletionRate = totalAttended > 0 ? Math.Round((double)_reverseStep1Completed / totalAttended * 100, 1) : 0;
            _reverseStep1Status = _reverseStep1Completed > 0 ? "Active" : "No Data";
            _reverseStep1Color = _reverseStep1CompletionRate >= 95 ? Color.Success : _reverseStep1CompletionRate >= 70 ? Color.Warning : Color.Info;
        }

        private async Task LoadCentersSyncStatusAsync()
        {
            var response = await SyncToExamServer.GetCentersSyncStatusAsync();

            if (response.CustomCodeStatus != CustomCodeStatus.Success || response.Data == null)
                return;

            var data = response.Data is JsonElement json
                ? JsonSerializer.Deserialize<List<CentersSyncStatusResponseDto>>(json.GetRawText(), _jsonOptions)
                : response.Data as List<CentersSyncStatusResponseDto>;

            if (data == null)
                return;

            _centerStatuses = data.ConvertAll(c => new CenterSyncStatus
            {
                CenterCode = c.CenterCode ?? string.Empty,
                CenterName = c.CenterName ?? string.Empty,
                Region = c.Region ?? "KSA",
                ExpectedExaminees = c.CBTReceived,
                ImportedToCentral = c.CBTReceived,
                PushedToServer = c.Pushed,
                AckReceived = c.Acknowledged,
                Status = c.Status,
                LastSuccessfulSync = c.LastSync
            });
        }

        private async Task SyncStatusOESToCESReportAsync()
        {
            var response = await BlazCandidatesResult.GetSyncStatusOESToCESReportAsync();

            if (response.CustomCodeStatus == CustomCodeStatus.Success && response.Data != null)
            {
                _syncStatusOESToCESReportList = response.Data as List<SyncStatusOESToCESReportDto>;
                _syncStatusOESToCESReport = _syncStatusOESToCESReportList.FirstOrDefault() ?? new();
            }
            else
            {
                _syncStatusOESToCESReport = new();
            }
        }

        private async Task SyncStatusCESToOESReportAsync()
        {
            var response = await BlazCandidatesResult.GetSyncStatusCESToOESReportAsync();

            if (response.CustomCodeStatus == CustomCodeStatus.Success && response.Data != null)
            {
                _syncStatusCESToOESReportList = response.Data as List<SyncStatusCESToOESReportDto>;
                _syncStatusCESToOESReport = _syncStatusCESToOESReportList.FirstOrDefault() ?? new();
            }
            else
            {
                _syncStatusCESToOESReport = new();
            }
        }

        private void LoadMockData()
        {
            // Generate mock chart data
            GenerateMockChartData();

            // Generate mock error data
            GenerateMockErrorData();

            // Generate mock alerts
            GenerateMockAlerts();
        }

        private void GenerateMockChartData()
        {
            // Volume Trend (Last 30 days)
            var dates = Enumerable.Range(0, 30)
                .Select(i => DateTime.Today.AddDays(-29 + i).ToString("MM/dd"))
                .ToArray();

            _volumeTrendLabels = dates;

            var random = new Random(42);
            var pulledData = Enumerable.Range(0, 30).Select(_ => (double)random.Next(14000, 16000)).ToArray();
            var importedData = pulledData.Select(x => x - random.Next(20, 60)).ToArray();
            var distributedData = importedData.Select(x => x - random.Next(50, 150)).ToArray();
            var ackedData = distributedData.Select(x => x - random.Next(100, 300)).ToArray();

            _volumeTrendSeries = new List<ChartSeries>
            {
                new ChartSeries { Name = "Pulled from CBT", Data = pulledData },
                new ChartSeries { Name = "Imported to Central", Data = importedData },
                new ChartSeries { Name = "Distributed to Centers", Data = distributedData },
                new ChartSeries { Name = "Acknowledged", Data = ackedData }
            };

            // Latency Distribution
            _latencyLabels = new[] { "< 200ms", "200-500ms", "500-1000ms", "1000-2000ms", "> 2000ms" };
            _latencyData = new double[] { 15, 35, 25, 18, 7 };

            // Volume by Region
            _regionLabels = new[] { "North", "South", "East", "West", "Central" };
            var expectedByRegion = new double[] { 3200, 2850, 3100, 2900, 3370 };
            var ackedByRegion = new double[] { 3180, 2820, 3070, 2850, 3330 };

            _regionVolumeSeries = new List<ChartSeries>
            {
                new ChartSeries { Name = "Expected", Data = expectedByRegion },
                new ChartSeries { Name = "Acknowledged", Data = ackedByRegion }
            };
        }

        private void GenerateMockErrorData()
        {
            _errorSummaries = new List<ErrorSummary>
            {
                new ErrorSummary
                {
                    ErrorCode = "ERR_TIMEOUT_503",
                    Description = "Server connection timeout",
                    CountToday = 12,
                    ImpactedCenters = 3,
                    ImpactedExaminees = 25
                },
                new ErrorSummary
                {
                    ErrorCode = "ERR_VALIDATION_400",
                    Description = "Invalid examinee data format",
                    CountToday = 8,
                    ImpactedCenters = 5,
                    ImpactedExaminees = 15
                },
                new ErrorSummary
                {
                    ErrorCode = "ERR_AUTH_401",
                    Description = "Authentication failed",
                    CountToday = 3,
                    ImpactedCenters = 2,
                    ImpactedExaminees = 8
                }
            };

            var random = new Random(42);

            _failedJobsList = new List<FailedJob>
            {
                new FailedJob
                {
                    JobId = "JOB-2025-001",
                    CenterCode = "CTR-073",
                    SyncStage = "Distribution",
                    StartTime = DateTimeHelper.Now.AddMinutes(-45),
                    EndTime = DateTimeHelper.Now.AddMinutes(-43),
                    Status = "Failed",
                    ErrorMessage = "Connection timeout after 3 retries"
                },
                new FailedJob
                {
                    JobId = "JOB-2025-002",
                    CenterCode = "CTR-074",
                    SyncStage = "Acknowledgment",
                    StartTime = DateTimeHelper.Now.AddMinutes(-30),
                    EndTime = null,
                    Status = "RetryScheduled",
                    ErrorMessage = "Server not responding"
                },
                new FailedJob
                {
                    JobId = "JOB-2025-003",
                    CenterCode = "CTR-075",
                    SyncStage = "Central Processing",
                    StartTime = DateTimeHelper.Now.AddMinutes(-15),
                    EndTime = DateTimeHelper.Now.AddMinutes(-10),
                    Status = "RetrySuccess",
                    ErrorMessage = "Temporary database lock (resolved)"
                }
            };
        }

        private void GenerateMockAlerts()
        {
            _activeAlerts = new List<AlertInfo>
            {
                new AlertInfo
                {
                    Severity = Severity.Error,
                    Title = "Center Offline",
                    Message = "3 centers (CTR-073, CTR-074, CTR-075) have not responded for > 30 minutes"
                },
                new AlertInfo
                {
                    Severity = Severity.Warning,
                    Title = "Latency Breach",
                    Message = "Average sync latency exceeded 400ms threshold (current: 450ms)"
                },
                new AlertInfo
                {
                    Severity = Severity.Info,
                    Title = "Partial Sync Detected",
                    Message = "4 centers completed with partial data. Manual verification recommended."
                }
            };
        }

        // Helper Methods
        private Color GetSyncStatusColor()
        {
            return _syncStatusToday switch
            {
                "Completed" => Color.Success,
                "Running" => Color.Info,
                "Partial" => Color.Warning,
                "Failed" => Color.Error,
                _ => Color.Default
            };
        }

        private Color GetReverseSyncStatusColor()
        {
            return _reverseSyncStatusToday switch
            {
                "Completed" => Color.Success,
                "Running" => Color.Info,
                "Partial" => Color.Warning,
                "Failed" => Color.Error,
                _ => Color.Default
            };
        }

        private double GetLatencyProgress()
        {
            return Math.Min(100, (_avgLatency / (double)_maxLatency) * 100);
        }

        private double GetAckPercentage(CenterSyncStatus center)
        {
            if (center.ExpectedExaminees == 0) return 0;
            return Math.Round((center.AckReceived / (double)center.ExpectedExaminees) * 100, 1);
        }

        private Color GetAckProgressColor(CenterSyncStatus center)
        {
            var percentage = GetAckPercentage(center);
            return percentage >= 95 ? Color.Success :
                   percentage >= 80 ? Color.Warning :
                   Color.Error;
        }

        private Color GetStatusColor(SyncJobStatus status)
        {
            return status switch
            {
                SyncJobStatus.Success => Color.Success,
                SyncJobStatus.InProgress => Color.Info,
                SyncJobStatus.Pending => Color.Warning,
                SyncJobStatus.Failed => Color.Error,
                _ => Color.Default
            };
        }

        private Color GetJobStatusColor(string status)
        {
            return status switch
            {
                "Failed" => Color.Error,
                "RetryScheduled" => Color.Warning,
                "RetrySuccess" => Color.Success,
                _ => Color.Default
            };
        }

        private void ToggleStatusFilter(SyncJobStatus status)
        {
            if (_tableStatusFilters.Contains(status))
            {
                _tableStatusFilters.Remove(status);
            }
            else
            {
                _tableStatusFilters.Add(status);
            }
        }

        private IEnumerable<CenterSyncStatus> GetSortedAndFilteredCenters()
        {
            var filtered = _centerStatuses.AsEnumerable();

            // Filter by selected center
            if (!string.IsNullOrWhiteSpace(_selectedCenter))
            {
                filtered = filtered.Where(c =>
                    c.CenterCode.Contains(_selectedCenter, StringComparison.OrdinalIgnoreCase) ||
                    c.CenterName.Contains(_selectedCenter, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by table status chips
            if (_tableStatusFilters.Count > 0)
            {
                filtered = filtered.Where(c => _tableStatusFilters.Contains(c.Status));
            }

            // Sort by status priority: Failed > Pending > InProgress > Success
            return filtered.OrderBy(c =>
            {
                return c.Status switch
                {
                    SyncJobStatus.Failed => 1,
                    SyncJobStatus.Pending => 2,
                    SyncJobStatus.InProgress => 3,
                    SyncJobStatus.Success => 4,
                    _ => 5
                };
            }).ThenBy(c => c.CenterCode);
        }

        private Task<IEnumerable<string>> SearchCenters(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Task.FromResult(_centerStatuses.Select(c => c.CenterCode).AsEnumerable());
            }

            var results = _centerStatuses
                .Where(c => c.CenterCode.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                            c.CenterName.Contains(value, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.CenterCode);

            return Task.FromResult(results);
        }

        private async Task OpenCenterDetailDialog(CenterSyncStatus center)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.ScheduleCbtViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters
            {
                ["CenterStatus"] = center
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseOnEscapeKey = true
            };

            await DialogService.ShowAsync<CenterDetailDialog>(
                $"Center Details - {center.CenterCode}",
                parameters,
                options
            );
        }

        private async Task OnManualSyncClicked()
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.ScheduleCbtSyncer);
            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            if (!_selectedDate.HasValue)
            {
                Snackbar.Add(Resource.PleaseSelectExamDateFirst, Severity.Warning);
                return;
            }

            if (_isSyncRunning)
            {
                Snackbar.Add(Resource.SyncInProgress, Severity.Warning);
                return;
            }

            var dialog = await DialogService.ShowAsync<ManualSyncOptionsDialog>(
                Resource.StartManualSync,
                new DialogParameters { [nameof(ManualSyncOptionsDialog.Venues)] = _centerStatuses },
                new DialogOptions
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true
                }
            );

            var result = await dialog.Result;
            if (result is null || result.Canceled) return;

            var (syncAll, venueCodes, numberOfDays) = ((bool, List<string>, int))result.Data!;

            if (syncAll)
            {
                var response = await SyncToExamServer.StartManualSyncAsync(numberOfDays);
                _isSyncRunning = response.CustomCodeStatus == CustomCodeStatus.Success;
                Snackbar.Add(response.Message, _isSyncRunning ? Severity.Info : Severity.Warning);
            }
            else
            {
                var hasNoCandidates = true;

                foreach (var code in venueCodes)
                {
                    for (int i = 0; i < numberOfDays; i++)
                    {
                        var response = await SyncToExamServer.SyncByVenueAndDateAsync(code, _selectedDate.Value.AddDays(i));

                        if (response.CustomCodeStatus == CustomCodeStatus.Success &&
                            response.Message != Resource.NoCandidatesFound &&
                            response.Message != Resource.NoCandidatesFoundFromCBTAPIForVenueAndDate)
                        {
                            hasNoCandidates = false;
                        }
                    }
                }

                if (hasNoCandidates)
                    Snackbar.Add(Resource.NoCandidatesFound, Severity.Error);
                else
                    Snackbar.Add(Resource.SyncInProgress, Severity.Info);
            }

            StateHasChanged();

            //var response = await SyncToExamServer.ManualSyncAllVenuesAsync();

            //Snackbar.Add(response.Message, Severity.Success);

            //// Start polling to refresh data
            //StartPolling();
        }

        private async Task OnManualSyncCandidateAnswersClicked()
        {
            var authorized = await AuthService
                .IsCurrentUserInRoleAsync(OesTemplateRoleConstants.CandidateAnswersToEvaluationSyncer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            if (!_selectedDate.HasValue)
            {
                Snackbar.Add(Resource.PleaseSelectExamDateFirst, Severity.Warning);
                return;
            }

            var response = await BlazSyncToEvaluation.ManualSyncCandidateAnswersAsync();

            Snackbar.Add(response.Message, Severity.Success);
        }

        private void StartPolling()
        {
            if (_isPolling) return;

            _isPolling = true;

            _refreshTimer = new Timer(async _ =>
            {
                await InvokeAsync(async () =>
                {
                    await LoadCBTSyncStatsAsync();
                    await LoadExamCompletionStatsAsync();
                    await LoadCentersSyncStatusAsync();
                    StateHasChanged();
                });
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
        }

        private void StopPolling()
        {
            _refreshTimer?.Dispose();
            _refreshTimer = null;
            _isPolling = false;
        }
    }

    // Data Models
    public class CenterSyncStatus
    {
        public string CenterCode { get; set; } = string.Empty;
        public string CenterName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public int ExpectedExaminees { get; set; }
        public int ImportedToCentral { get; set; }
        public int PushedToServer { get; set; }
        public int AckReceived { get; set; }
        public SyncJobStatus Status { get; set; }
        public DateTime? LastSuccessfulSync { get; set; }
        public string? LastError { get; set; }
    }

    public class ErrorSummary
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CountToday { get; set; }
        public int ImpactedCenters { get; set; }
        public int ImpactedExaminees { get; set; }
    }

    public class FailedJob
    {
        public string JobId { get; set; } = string.Empty;
        public string CenterCode { get; set; } = string.Empty;
        public string SyncStage { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class AlertInfo
    {
        public Severity Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}