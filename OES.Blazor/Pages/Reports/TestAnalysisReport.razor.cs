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
    public partial class TestAnalysisReport
    {
        private static readonly string _infoOverview = Resource.TestAnalysisReportOverview;

        private static readonly List<ReportInfoSection> _infoSections =
        [
            new ReportInfoSection(Resource.RptTA_SectionFreqTable,
            [
                new ReportInfoItem(Resource.RptTA_ScoreRangePct, Resource.RptTA_ScoreRangePctDesc),
                new ReportInfoItem(Resource.RptTA_RawScoreRange, Resource.RptTA_RawScoreRangeDesc),
                new ReportInfoItem(Resource.RptTA_Frequency, Resource.RptTA_FrequencyDesc),
                new ReportInfoItem(Resource.RptTA_Percent, Resource.RptTA_PercentDesc),
            ]),
            new ReportInfoSection(Resource.RptSD_SectionHistogram,
            [
                new ReportInfoItem(Resource.RptItemBars, Resource.RptTA_BarsDesc),
            ]),
            new ReportInfoSection(Resource.RptTA_SectionStatSummary,
            [
                new ReportInfoItem(Resource.RptTA_NoOfCandidatesItems, Resource.RptTA_NoOfCandidatesItemsDesc),
                new ReportInfoItem(Resource.RptTA_MinMaxScore, Resource.RptTA_MinMaxScoreDesc),
                new ReportInfoItem(Resource.RptTA_MeanMedianMode, Resource.RptTA_MeanMedianModeDesc),
                new ReportInfoItem(Resource.RptStdDeviation, Resource.RptTA_StdDeviationDesc),
                new ReportInfoItem(Resource.RptVariance, Resource.RptTA_VarianceDesc),
                new ReportInfoItem(Resource.RptKr20, Resource.RptTA_KR20Desc),
                new ReportInfoItem(Resource.RptStdErrorOfMean, Resource.RptTA_StdErrorMeanDesc),
                new ReportInfoItem(Resource.RptStdErrorOfMeasurement, Resource.RptTA_StdErrorMeasurementDesc),
                new ReportInfoItem(Resource.RptSkewness, Resource.RptSD_SkewnessDesc),
                new ReportInfoItem(Resource.RptKurtosis, Resource.RptSD_KurtosisDesc),
            ]),
        ];

        [Inject] private IBlazAnalyticalReportsService AnalyticalReportsService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private static readonly ChartOptions _chartOptions = new()
        {
            YAxisTicks = 1
        };
        private static readonly TestAnalysisStatsDto _emptyStats = new(
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            0,
            null,
            0,
            0);
        private static readonly TestAnalysisReportDto _emptyReport = new(
            string.Empty,
            string.Empty,
            string.Empty,
            [],
            _emptyStats);
        private TestAnalysisReportDto _report;
        private bool _isLoading;
        private bool _isExporting;
        private string _errorMessage;
        private AnalyticalReportFilterDto _currentFilter = new();

        private bool HasReport => _report is not null && (_report.FrequencyBuckets.Count > 0 || _report.Stats.NoOfCandidates > 0);
        private bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);
        private bool HasBuckets => _report?.FrequencyBuckets.Count > 0;
        private string[] ChartLabels => _report?.FrequencyBuckets.Select(x => x.PercentScoreRange).ToArray() ?? [];
        private List<ChartSeries> ChartSeries => _report is null ? [] :
        [
            new()
            {
                Name = Resource.RptCandidates,
                Data = [.. _report.FrequencyBuckets.Select(x => (double)x.Frequency)]
            }
        ];


        private Task OnFilterChangedAsync(AnalyticalReportFilterDto filter)
        {
            _currentFilter = filter;

            ResetReport();

            return Task.CompletedTask;
        }

        private async Task GenerateAsync() => await LoadReportAsync();

        private async Task LoadReportAsync()
        {
            if (_currentFilter.ScheduleIds.Count == 0 || _currentFilter.PaperCodes.Count == 0)
            {
                return;
            }

            ResetError();

            ResetReport();

            _isLoading = true;

            await InvokeAsync(StateHasChanged);

            try
            {
                _report = await AnalyticalReportsService.GetTestAnalysisReportAsync(
                    new TestAnalysisReportRequestDto(
                        _currentFilter.ScheduleIds,
                        _currentFilter.PaperCodes,
                        _currentFilter.VenueCodes));
            }
            catch (Exception exception)
            {
                _errorMessage = string.Format(Resource.RptFailedToLoad, exception.Message);
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
                var frequencySheet = BuildFrequencySheet(_report);

                var statisticsSheet = BuildStatisticsSheet(_report);

                var (bytes, extension) = ReportExportHelper.Build(
                    Resource.RptTitleTestAnalysis,
                    frequencySheet,
                    statisticsSheet);

                await JS.InvokeVoidAsync("downloadFileFromBytes", bytes, $"Test_Analysis_Report.{extension}");
            }
            finally
            {
                _isExporting = false;

                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task PrintAsync()
        {
            await JS.InvokeVoidAsync("window.print");
        }

        private void ClearError()
        {
            _errorMessage = null;
        }

        private void ResetError()
        {
            _errorMessage = null;
        }

        private void ResetReport()
        {
            _report = null;
        }

        private static ExportSheet BuildFrequencySheet(TestAnalysisReportDto report)
        {
            return new ExportSheet(
                Resource.RptScoreFreqDistribution,
                [
                    Resource.RptScoreRange,
                    Resource.RptRawScoreRange,
                    Resource.RptFrequency,
                    Resource.RptPercent
                ],
                report.FrequencyBuckets.Select(
                    x => (IEnumerable<object?>)
                    [
                        x.PercentScoreRange,
                        x.RawScoreRange,
                        x.Frequency,
                        x.Percent
                    ]));
        }

        private static ExportSheet BuildStatisticsSheet(TestAnalysisReportDto report)
        {
            var stats = report.Stats ?? _emptyStats;

            return new ExportSheet(
                Resource.RptStatisticalSummary,
                [
                    Resource.RptMetric,
                    Resource.RptValue
                ],
                [
                    [Resource.RptNoOfCandidates, stats.NoOfCandidates],
                    [Resource.RptNoOfItems, stats.NoOfItems],
                    [Resource.RptMinScore, stats.MinScore],
                    [Resource.RptMaxScore, stats.MaxScore],
                    [Resource.RptMean, stats.Mean],
                    [Resource.RptMedian, stats.Median],
                    [Resource.RptMode, stats.Mode],
                    [Resource.RptStdDeviation, stats.StdDev],
                    [Resource.RptVariance, stats.Variance],
                    [Resource.RptKr20, stats.CronbachAlpha],
                    [Resource.RptStdErrorOfMean, stats.StdErrorOfMean],
                    [Resource.RptStdErrorOfMeasurement, stats.StdErrorOfMeasurement],
                    [Resource.RptSkewness, stats.Skew],
                    [Resource.RptKurtosis, stats.Kurtosis]
                ]);
        }
    }
}
