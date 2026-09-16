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
    public partial class PerformanceSummaryReport
    {
        private static readonly string _infoOverview = Resource.PerformanceSummaryReportOverview;

        private static readonly List<ReportInfoSection> _infoSections =
        [
            new ReportInfoSection(Resource.RptPS_SectionScoreTiles,
            [
                new ReportInfoItem(Resource.RptPS_AverageScore, Resource.RptPS_AverageScoreDesc),
                new ReportInfoItem(Resource.RptPS_LowScore, Resource.RptPS_LowScoreDesc),
                new ReportInfoItem(Resource.RptPS_HighScore, Resource.RptPS_HighScoreDesc),
            ]),
            new ReportInfoSection(Resource.RptPS_SectionKR20Bar,
            [
                new ReportInfoItem(Resource.RptPS_KR20Value, Resource.RptPS_KR20ValueDesc),
            ]),
            new ReportInfoSection(Resource.RptPS_SectionLOTable,
            [
                new ReportInfoItem(Resource.RptPS_CategoryILO, Resource.RptPS_CategoryILODesc),
                new ReportInfoItem(Resource.RptPS_AvgCorrectPct, Resource.RptPS_AvgCorrectPctDesc),
            ]),
            new ReportInfoSection(Resource.RptPS_SectionAtRisk,
            [
                new ReportInfoItem(Resource.RptPS_AtRiskGroup, Resource.RptPS_AtRiskGroupDesc),
            ]),
            new ReportInfoSection(Resource.RptPS_SectionQPerfTable,
            [
                new ReportInfoItem(Resource.RptPS_CorrectPct, Resource.RptPS_CorrectPctDesc),
                new ReportInfoItem(Resource.RptPS_UpperLower27, Resource.RptPS_UpperLower27Desc),
                new ReportInfoItem(Resource.RptPtBiserial, Resource.RptPS_PtBiserialDesc),
                new ReportInfoItem(Resource.RptPS_DiscriminationIndex, Resource.RptPS_DiscriminationIndexDesc),
                new ReportInfoItem(Resource.RptPS_OptionPct, Resource.RptPS_OptionPctDesc),
            ]),
            new ReportInfoSection(Resource.RptPS_SectionHistogram,
            [
                new ReportInfoItem(Resource.RptItemBars, Resource.RptPS_BarsDesc),
            ]),
        ];

        [Inject] private IBlazAnalyticalReportsService AnalyticalReportsService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private PerformanceSummaryReportDto? _report;
        private bool _isLoading;
        private bool _isExporting;
        private string? _errorMessage;
        private AnalyticalReportFilterDto _currentFilter = new();
        private static readonly ChartOptions _chartOptions = new()
        {
            YAxisTicks = 1
        };

        private bool HasReport => _report is not null;
        private bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);
        private bool HasHistogram => (bool)(_report?.HistogramBuckets.Any(x => x.Frequency > 0));

        private string[] ChartLabels => _report?.HistogramBuckets.Select(x => x.PercentScoreRange).ToArray() ?? [];
        private List<ChartSeries> ChartSeries =>
        [
            new()
            {
                Name = Resource.RptCandidates,
                Data = _report?.HistogramBuckets.Select(x => (double)x.Frequency).ToArray() ?? []
            }
        ];

        private static ChartOptions ChartOptions => _chartOptions;


        private Task OnFilterChangedAsync(AnalyticalReportFilterDto filter)
        {
            _currentFilter = filter;

            ResetReport();

            return Task.CompletedTask;
        }

        private async Task GenerateAsync() => await LoadReportAsync();

        private async Task LoadReportAsync()
        {
            ResetError();

            _isLoading = true;

            await InvokeAsync(StateHasChanged);

            try
            {
                _report = await AnalyticalReportsService.GetPerformanceSummaryReportAsync(
                    new PerformanceSummaryReportRequestDto(
                        _currentFilter.ScheduleIds,
                        _currentFilter.VenueCodes,
                        _currentFilter.PaperCodes));
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
                var summarySheet = BuildSummarySheet(_report);
                var outcomesSheet = BuildLearningOutcomesSheet(_report);
                var atRiskSheet = BuildAtRiskStudentsSheet(_report);
                var performanceSheet = BuildQuestionPerformanceSheet(_report);

                var (bytes, extension) = ReportExportHelper.Build(
                    Resource.RptTitlePerformanceSummary,
                    summarySheet,
                    outcomesSheet,
                    atRiskSheet,
                    performanceSheet);

                await JS.InvokeVoidAsync("downloadFileFromBytes", bytes, $"Performance_Summary_Report.{extension}");
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

        private static ExportSheet BuildSummarySheet(PerformanceSummaryReportDto report)
        {
            return new ExportSheet(
                Resource.RptTitlePerformanceSummary,
                [Resource.RptMetric, Resource.RptValue],
                [
                    [Resource.RptTotalQuestions, report.TotalQuestions],
                    [Resource.RptExamTakers, report.ExamTakers],
                    [Resource.RptAverageScore, report.AverageScorePct],
                    [Resource.RptLowScore, report.LowScorePct],
                    [Resource.RptHighScore, report.HighScorePct],
                    [Resource.RptKr20Reliability, report.Kr20]
                ]);
        }

        private static ExportSheet BuildLearningOutcomesSheet(PerformanceSummaryReportDto report)
        {
            return new ExportSheet(
                Resource.RptLearningOutcomes,
                [
                    Resource.Category,
                    Resource.RptAvgCorrectPct
                ],
                report.LearningOutcomes.Select(
                    x => (IEnumerable<object?>)
                    [
                        x.CategoryName,
                        x.AvgCorrectPct
                    ]));
        }

        private static ExportSheet BuildAtRiskStudentsSheet(PerformanceSummaryReportDto report)
        {
            return new ExportSheet(
                Resource.RptAtRiskStudents,
                [
                    Resource.RptCandidateCode,
                    Resource.Name,
                    Resource.RptRawScore,
                    Resource.RptScorePct
                ],
                report.AtRiskStudents.Select(
                    x => (IEnumerable<object?>)
                    [
                        x.CandidateCode,
                        x.DisplayName,
                        x.RawScore,
                        x.ScorePct
                    ]));
        }

        private static ExportSheet BuildQuestionPerformanceSheet(PerformanceSummaryReportDto report)
        {
            return new ExportSheet(
                Resource.RptQuestionPerformance,
                [
                    "#",
                    Resource.RptQuestionCode,
                    Resource.RptItemStem,
                    Resource.RptCorrectPct,
                    Resource.RptUpper27Pct,
                    Resource.RptLower27Pct,
                    Resource.RptPtBiserial,
                    Resource.RptDiscIndex,
                    "A%",
                    "B%",
                    "C%",
                    "D%",
                    "E%",
                    "F%"
                ],
                report.QuestionPerformance.Select(
                    x => (IEnumerable<object?>)
                    [
                        x.SrNo,
                        x.QuestionCode,
                        x.ItemStem,
                        x.CorrectPct,
                        x.Upper27Pct,
                        x.Lower27Pct,
                        x.PointBiserial,
                        x.DiscIndex,
                        x.FreqA,
                        x.FreqB,
                        x.FreqC,
                        x.FreqD,
                        x.FreqE,
                        x.FreqF
                    ]));
        }
    }
}
