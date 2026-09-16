using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.InternalHelperTypes.General;
using OES.Blazor.Services;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Results;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Reports
{
    public partial class CentersAllocationVerification : IDisposable
    {
        private static readonly string _infoOverview = Resource.CentersAllocationVerificationOverview;

        private static readonly List<ReportInfoSection> _infoSections =
        [
            new ReportInfoSection(Resource.SummaryTable,
            [
                new ReportInfoItem(Resource.CentersConnected, Resource.CentersConnectedDescription),
                new ReportInfoItem(Resource.TotalExpected, Resource.TotalExpectedDescription),
                new ReportInfoItem(Resource.TotalLocalCount, Resource.TotalLocalCountDescription),
                new ReportInfoItem(Resource.MatchStatus, Resource.MatchStatusDescription),
            ]),

            new ReportInfoSection(Resource.DetailsTable,
            [
                new ReportInfoItem(Resource.TotalExpected, Resource.TotalExpectedDescription),
                new ReportInfoItem(Resource.Synced, Resource.SyncedDescription),
                new ReportInfoItem(Resource.NotSynced, Resource.NotSyncedDescription),
                new ReportInfoItem(Resource.LocalCount, Resource.LocalCountDescription),
                new ReportInfoItem(Resource.Match, Resource.MatchDescription),
                new ReportInfoItem(Resource.Mismatch, Resource.MismatchDescription),
                new ReportInfoItem(Resource.Error, Resource.ErrorDescription),
            ]),
        ];

        [Inject] private IBlazVerificationReportsService Service { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private DateTime? _examDate = DateTime.Today.AddDays(1);
        private List<CentersAllocationVerificationResultDto> _results = [];
        private bool _isLoading;
        private bool _isExporting;
        private string? _errorMessage;
        private CancellationTokenSource? _cts;

        private VerificationSummary Summary => BuildSummary(_results);

        private async Task GenerateAsync()
        {
            if (_examDate is null)
            {
                Snackbar.Add(Resource.PleaseSelectExamDateFirst, Severity.Warning);

                return;
            }

            ResetState();

            _isLoading = true;

            try
            {
                await LoadResultsAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _errorMessage = $"{Resource.ReportGenerationFailed}: {ex.Message}";
            }
            finally
            {
                _isLoading = false;

                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task LoadResultsAsync()
        {
            var request = new CentersAllocationVerificationRequest
            {
                ExamDate = DateOnly.FromDateTime(_examDate!.Value)
            };

            await foreach (var result in Service.GetCentersAllocationVerificationReportStreamAsync(request, _cts!.Token))
            {
                _results.Add(result);

                await InvokeAsync(StateHasChanged);
            }

            _results = [.. _results.OrderBy(x => x.VenueName)];
        }

        private async Task ExportAsync()
        {
            if (_results is null || _examDate is null)
            {
                return;
            }

            _isExporting = true;
            StateHasChanged();

            try
            {
                var summary = Summary;
                var dateStr = _examDate.Value.ToString("yyyy-MM-dd");

                var summarySheet = new ExportSheet(Resource.Summary,
                    [Resource.Metric, Resource.Value],
                    [
                        [Resource.ExamDate,            dateStr],
                        [Resource.TotalCenters,        _results.Count],
                        [Resource.CentersConnected,    $"{summary.ConnectedCenters} / {_results.Count}"],
                        [Resource.TotalExpectedCount,  summary.TotalExpected],
                        [Resource.TotalCenterDBCount,  summary.TotalLocal],
                        [Resource.Match,               summary.MatchCount],
                        [Resource.Mismatch,            summary.MismatchCount],
                        [Resource.Error,               summary.ErrorCount],
                    ]);

                var detailsSheet = new ExportSheet("Centers",
                    [
                        Resource.CenterName, Resource.CenterCode,
                        Resource.TotalExpected, Resource.SyncedCount, Resource.NotSyncedCount,
                        Resource.CenterDBCount, Resource.Status, Resource.Error
                    ],
                    _results.Select(r => (IEnumerable<object?>)
                    [
                        r.VenueName,
                        r.VenueCode,
                        r.TotalExpected,
                        r.SyncedCount,
                        r.NotSyncedCount,
                        r.LocalCount.HasValue ? (object?)r.LocalCount.Value : "—",
                        GetStatusLabel(r.Status),
                        FriendlyError(r.ErrorMessage)
                    ]));

                var (bytes, ext) = ReportExportHelper.Build(Resource.CentersAllocationVerification, summarySheet, detailsSheet);

                await JS.InvokeVoidAsync("downloadFileFromBytes", bytes, $"Centers_Allocation_Verification_{dateStr}.{ext}");
            }
            finally
            {
                _isExporting = false;
                StateHasChanged();
            }
        }

        private static Color GetStatusColor(string status) => status switch
        {
            nameof(VerificationStatus.Match) => Color.Success,
            nameof(VerificationStatus.Mismatch) => Color.Warning,
            _ => Color.Error
        };

        private static string GetStatusLabel(string status) => status switch
        {
            nameof(VerificationStatus.Match) => Resource.Match,
            nameof(VerificationStatus.Mismatch) => Resource.Mismatch,
            _ => Resource.Error
        };

        private static string FriendlyError(string? error)
        {
            if (string.IsNullOrEmpty(error)) return string.Empty;
            if (error.Contains("Unable to connect to any of the specified MySQL hosts", StringComparison.OrdinalIgnoreCase))
                return Resource.UnableToConnect;
            if (error.Contains("Connect Timeout expired", StringComparison.OrdinalIgnoreCase))
                return Resource.ConnectionTimedOut;
            if (error.Contains("Command Timeout expired", StringComparison.OrdinalIgnoreCase))
                return Resource.QueryTimedOut;
            return error;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private void ResetState()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _results.Clear();
            _errorMessage = null;
        }

        private static VerificationSummary BuildSummary(IReadOnlyCollection<CentersAllocationVerificationResultDto> results)
        {
            return new VerificationSummary
            {
                TotalCenters = results.Count,
                ConnectedCenters = results.Count(x => x.Status != nameof(VerificationStatus.Error)),
                TotalExpected = results.Sum(x => x.TotalExpected),
                TotalLocal = results.Where(x => x.LocalCount.HasValue).Sum(x => x.LocalCount!.Value),
                MatchCount = results.Count(x => x.Status == nameof(VerificationStatus.Match)),
                MismatchCount = results.Count(x => x.Status == nameof(VerificationStatus.Mismatch)),
                ErrorCount = results.Count(x => x.Status == nameof(VerificationStatus.Error))
            };
        }

        private sealed record VerificationSummary
        {
            public int TotalCenters { get; init; }

            public int ConnectedCenters { get; init; }

            public int TotalExpected { get; init; }

            public int TotalLocal { get; init; }

            public int MatchCount { get; init; }

            public int MismatchCount { get; init; }

            public int ErrorCount { get; init; }

            public bool AllCentersFailed => TotalCenters > 0 && ErrorCount == TotalCenters;

            public Color MatchStatusColor => MatchCount == TotalCenters ? Color.Success : Color.Warning;
        }
    }
}
