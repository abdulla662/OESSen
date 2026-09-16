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
    public partial class VenueAttendanceVerification : IDisposable
    {
        private static readonly string _infoOverview = Resource.VenueAttendanceVerificationOverview;

        private static readonly List<ReportInfoSection> _infoSections =
        [
            new ReportInfoSection(Resource.RptVV_SectionSummaryTable,
            [
                new ReportInfoItem(Resource.RptVV_CentersConnected, Resource.RptVV_CentersConnectedDesc),
                new ReportInfoItem(Resource.RptVV_CentralCount, Resource.RptVV_CentralCountDesc),
                new ReportInfoItem(Resource.RptVV_LocalCount, Resource.RptVV_LocalCountDesc),
                new ReportInfoItem(Resource.RptVV_MatchStatus, Resource.RptVV_MatchStatusDesc),
            ]),
            new ReportInfoSection(Resource.RptVV_SectionDetailsTable,
            [
                new ReportInfoItem(Resource.RptVV_Match, Resource.RptVV_MatchDesc),
                new ReportInfoItem(Resource.RptVV_Mismatch, Resource.RptVV_MismatchDesc),
                new ReportInfoItem(Resource.Error, Resource.RptVV_ErrorDesc),
            ]),
        ];

        [Inject] private IBlazVerificationReportsService Service { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private DateTime? _examDate = DateTime.Today;
        private List<VenueAttendanceVerificationResultDto> _results = [];
        private bool _isLoading;
        private bool _isExporting;
        private string? _errorMessage;
        private CancellationTokenSource? _cancellationTokenSource;

        private VerificationReportSummary Summary => BuildSummary(_results);


        private async Task GenerateAsync()
        {
            if (_examDate is null)
            {
                Snackbar.Add(Resource.PleaseSelectExamDateFirst, Severity.Warning);
                return;
            }

            ResetState();

            _isLoading = true;

            StateHasChanged();

            try
            {
                var request = new VenueAttendanceVerificationRequest { ExamDate = DateOnly.FromDateTime(_examDate.Value) };

                await foreach (var result in Service.GetVenueAttendanceVerificationReportStreamAsync(request, _cancellationTokenSource.Token))
                {
                    _results.Add(result);
                    await InvokeAsync(StateHasChanged);
                }

                _results = [.. _results.OrderBy(x => x.VenueName)];
            }
            catch (OperationCanceledException) { }
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

        private async Task ExportAsync()
        {
            if (_results is null || _examDate is null)
            {
                return;
            }

            _isExporting = true;

            try
            {
                var exportDate = _examDate.Value.ToString("yyyy-MM-dd");

                var summarySheet = BuildSummarySheet(exportDate);

                var detailsSheet = BuildDetailsSheet();

                var (bytes, extension) = ReportExportHelper.Build(Resource.CenterAttendanceVerification, summarySheet, detailsSheet);

                await JS.InvokeVoidAsync("downloadFileFromBytes", bytes, $"Center_Attendance_Verification_{exportDate}.{extension}");
            }
            finally
            {
                _isExporting = false;
            }
        }

        private ExportSheet BuildSummarySheet(string exportDate)
        {
            return new ExportSheet(
                Resource.Summary,
                [Resource.Metric, Resource.Value],
                [
                    [Resource.ExamDate, exportDate],
                    [Resource.TotalCenters, Summary.TotalCenters],
                    [
                        Resource.CentersConnected,
                        $"{Summary.ConnectedCenters} / {Summary.TotalCenters}"
                    ],
                    [Resource.TotalCentralDBCount, Summary.TotalCentralCount],
                    [Resource.TotalCenterDBCount, Summary.TotalVenueCount],
                    [Resource.Match, Summary.MatchCount],
                    [Resource.Mismatch, Summary.MismatchCount],
                    [Resource.Error, Summary.ErrorCount]
                ]);
        }

        private ExportSheet BuildDetailsSheet()
        {
            return new ExportSheet(
                Resource.Centers,
                [
                    Resource.CenterName,
                    Resource.CenterCode,
                    Resource.CentralDBCount,
                    Resource.CenterDBCount,
                    Resource.Status,
                    Resource.Error
                ],
                _results.Select(
                    x => (IEnumerable<object?>)
                    [
                        x.VenueName,
                        x.VenueCode,
                        x.CentralCount,
                        x.VenueCount,
                        GetStatusLabel(x.Status),
                        FriendlyError(x.ErrorMessage)
                    ]));
        }

        private void ResetState()
        {
            _cancellationTokenSource?.Cancel();

            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = new CancellationTokenSource();

            _results = [];

            _errorMessage = null;
        }

        private static Color GetStatusColor(string status)
        {
            return status switch
            {
                nameof(VerificationStatus.Match) => Color.Success,
                nameof(VerificationStatus.Mismatch) => Color.Warning,
                _ => Color.Error
            };
        }

        private static string GetStatusLabel(string status)
        {
            return status switch
            {
                nameof(VerificationStatus.Match) => Resource.Match,
                nameof(VerificationStatus.Mismatch) => Resource.Mismatch,
                _ => Resource.Error
            };
        }

        private static string FriendlyError(string? error)
        {
            if (string.IsNullOrEmpty(error))
            {
                return string.Empty;
            }
            if (error.Contains("Unable to connect to any of the specified MySQL hosts", StringComparison.OrdinalIgnoreCase))
            {
                return Resource.UnableToConnect;
            }
            if (error.Contains("Connect Timeout expired", StringComparison.OrdinalIgnoreCase))
            {
                return Resource.ConnectionTimedOut;
            }
            if (error.Contains("Command Timeout expired", StringComparison.OrdinalIgnoreCase))
            {
                return Resource.QueryTimedOut;
            }
            return error;
        }

        private static VerificationReportSummary BuildSummary(IReadOnlyCollection<VenueAttendanceVerificationResultDto> results)
        {
            if (results.Count == 0)
            {
                return new();
            }

            return new VerificationReportSummary
            {
                TotalCenters = results.Count,
                ConnectedCenters = results.Count(x => x.Status != nameof(VerificationStatus.Error)),
                TotalCentralCount = results.Sum(x => x.CentralCount),
                TotalVenueCount = results.Where(x => x.VenueCount.HasValue).Sum(x => x.VenueCount!.Value),
                MatchCount = results.Count(x => x.Status == nameof(VerificationStatus.Match)),
                MismatchCount = results.Count(x => x.Status == nameof(VerificationStatus.Mismatch)),
                ErrorCount = results.Count(x => x.Status == nameof(VerificationStatus.Error))
            };
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        private sealed record VerificationReportSummary
        {
            public int TotalCenters { get; init; }
            public int ConnectedCenters { get; init; }
            public int TotalCentralCount { get; init; }
            public int TotalVenueCount { get; init; }
            public int MatchCount { get; init; }
            public int MismatchCount { get; init; }
            public int ErrorCount { get; init; }
            public bool AllCentersFailed => TotalCenters > 0 && ErrorCount == TotalCenters;
            public bool NoExamActivityFound => TotalCentralCount == 0 && TotalVenueCount == 0;
        }
    }
}
