using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.InternalHelperTypes.General;
using OES.Blazor.Services;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Reports
{
    public partial class CondensedTestReport
    {
        private const int ItemsPageSize = 100;
        private const string EmptyValue = "—";
        private static readonly string _infoOverview = Resource.CondensedTestReportOverview;

        private static readonly List<ReportInfoSection> _infoSections =
        [
            new ReportInfoSection(Resource.TestStatistics,
            [
                new ReportInfoItem(Resource.TotalPossiblePoints, Resource.TotalPossiblePointsDescription),
                new ReportInfoItem(Resource.TotalStudents, Resource.TotalStudentsDescription),
                new ReportInfoItem(Resource.MaxMinScore, Resource.MaxMinScoreDescription),
                new ReportInfoItem(Resource.Range, Resource.RangeDescription),
                new ReportInfoItem(Resource.Mean, Resource.MeanDescription),
                new ReportInfoItem(Resource.Median, Resource.MedianDescription),
                new ReportInfoItem(Resource.StdDeviation, Resource.StdDeviationDescription),
                new ReportInfoItem(Resource.KR20, Resource.KR20Description),
            ]),

            new ReportInfoSection(Resource.PerItemTable,
            [
                new ReportInfoItem(Resource.ItemOrder, Resource.ItemOrderDescription),
                new ReportInfoItem(Resource.QuestionCode, Resource.QuestionCodeDescription),
                new ReportInfoItem(Resource.Key, Resource.KeyDescription),
                new ReportInfoItem(Resource.OptionPercentages, Resource.OptionPercentagesDescription),
                new ReportInfoItem(Resource.NonDistractors, Resource.NonDistractorsDescription),
                new ReportInfoItem(Resource.CorrectPercentage, Resource.CorrectPercentageDescription),
                new ReportInfoItem(Resource.Upper27, Resource.Upper27Description),
                new ReportInfoItem(Resource.Lower27, Resource.Lower27Description),
                new ReportInfoItem(Resource.PtBiserial, Resource.PtBiserialDescription),
            ]),

            new ReportInfoSection(Resource.RowHighlighting,
            [
                new ReportInfoItem(Resource.HighlightedRows, Resource.HighlightedRowsDescription),
            ]),
        ];

        [Inject] private IBlazAnalyticalReportsService AnalyticalReportsService { get; set; } = default!;
        [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private CondensedTestReportDto? _report;
        private bool _isLoading;
        private bool _isExporting;
        private string? _errorMessage;
        private AnalyticalReportFilterDto _currentFilter = new();
        private int _itemsPage = 1;
        private CancellationTokenSource? _cts;

        private bool HasReport => _report is not null;

        private bool HasItems => _report?.Items.Count > 0;

        private int TotalPages => _report is null ? 0 : (int)Math.Ceiling(_report.Items.Count / (double)ItemsPageSize);

        private IEnumerable<CondensedTestItemDto> PageItems =>
            _report?.Items.Skip((_itemsPage - 1) * ItemsPageSize).Take(ItemsPageSize) ?? [];


        private Task OnFilterChangedAsync(AnalyticalReportFilterDto filter)
        {
            _currentFilter = filter;

            ResetReport();

            return Task.CompletedTask;
        }

        private async Task GenerateAsync()
        {
            if (_currentFilter.ScheduleIds.Count == 0)
            {
                Snackbar.Add(Resource.NoScheduleSelected, Severity.Warning);

                return;
            }

            if (_currentFilter.PaperCodes.Count == 0)
            {
                Snackbar.Add(Resource.PaperMustBeSelected, Severity.Warning);

                return;
            }

            await LoadReportAsync();
        }

        private async Task LoadReportAsync()
        {
            ResetReport();

            _cts?.Cancel();

            _cts?.Dispose();

            _cts = new CancellationTokenSource();

            _isLoading = true;

            await InvokeAsync(StateHasChanged);

            try
            {
                _report = await AnalyticalReportsService
                        .GetCondensedTestReportAsync(
                            new CondensedTestReportRequestDto
                            {
                                ScheduleIds = _currentFilter.ScheduleIds,
                                VenueCodes = _currentFilter.VenueCodes,
                                PaperCodes = _currentFilter.PaperCodes
                            }
                        );

                _itemsPage = 1;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _errorMessage = string.Format(Resource.RptFailedToLoad, ex.Message);
            }
            finally
            {
                _isLoading = false;

                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task ExportAsync()
        {
            if (_report is null)
            {
                return;
            }

            _isExporting = true;

            await InvokeAsync(StateHasChanged);

            try
            {
                var statisticsSheet = BuildStatisticsSheet();

                var itemAnalysisSheet = BuildItemAnalysisSheet();

                var (bytes, extension) = ReportExportHelper.Build(Resource.RptTitleCondensedTest, statisticsSheet, itemAnalysisSheet);

                await JsRuntime.InvokeVoidAsync("downloadFileFromBytes", bytes, $"Condensed_Test_Report.{extension}");
            }
            finally
            {
                _isExporting = false;

                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task PrintAsync()
        {
            await JsRuntime.InvokeVoidAsync("window.print");
        }

        private ExportSheet BuildStatisticsSheet()
        {
            return new ExportSheet(
                Resource.RptTestStatistics,
                [Resource.RptMetric, Resource.RptValue],
                [
                    [Resource.RptTotalPossiblePoints, _report!.TotalPossiblePoints],
                    [Resource.RptTotalStudents, _report.TotalStudents],
                    [Resource.RptMaxScore, _report.MaxScore],
                    [Resource.RptMinScore, _report.MinScore],
                    [Resource.RptRangeOfScores, _report.RangeOfScores],
                    [Resource.RptMeanScore, _report.MeanScore],
                    [Resource.RptMedianScore, _report.MedianScore],
                    [Resource.RptStdDeviation, _report.StdDev],
                    [Resource.RptKr20Reliability, _report.Kr20]
                ]);
        }

        private ExportSheet BuildItemAnalysisSheet()
        {
            return new ExportSheet(
                Resource.RptPerItemAnalysis,
                [
                    "#",
                    Resource.RptQuestionCode,
                    Resource.RptKey,
                    "A%", "B%", "C%", "D%", "E%", "F%",
                    Resource.RptNonDistractors,
                    Resource.RptCorrectPct,
                    Resource.RptUpper27Pct,
                    Resource.RptLower27Pct,
                    Resource.RptPtBiserial
                ],
                _report!.Items.Select(
                    x => (IEnumerable<object?>)
                    [
                        x.SrNo,
                        x.QuestionCode,
                        x.CorrectAnswer,
                        x.FreqA,
                        x.FreqB,
                        x.FreqC,
                        x.FreqD,
                        x.FreqE,
                        x.FreqF,
                        x.NonDistractors,
                        x.CorrectPct,
                        x.Upper27Pct,
                        x.Lower27Pct,
                        x.PointBiserial
                    ]));
        }

        private void ResetReport()
        {
            _report = null;

            _errorMessage = null;

            _itemsPage = 1;
        }

        private void ClearError()
        {
            _errorMessage = null;
        }

        private static string FormatPointBiserial(double? value)
        {
            return value?.ToString("F3") ?? EmptyValue;
        }

        public void Dispose()
        {
            _cts?.Cancel();

            _cts?.Dispose();
        }
    }
}