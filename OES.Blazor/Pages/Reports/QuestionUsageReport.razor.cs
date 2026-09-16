using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OES.Blazor.InternalHelperTypes.General;
using OES.Blazor.Services;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Reports
{
    public partial class QuestionUsageReport
    {
        private static readonly string _infoOverview = Resource.QuestionUsageReportOverview;

        private static readonly List<ReportInfoSection> _infoSections =
        [
            new ReportInfoSection(Resource.RptSectionTableColumns,
            [
                new ReportInfoItem(Resource.RptQuestionCode, Resource.RptQU_QuestionCodeDesc),
                new ReportInfoItem(Resource.RptQU_NoOfStudents, Resource.RptQU_NoOfStudentsDesc),
                new ReportInfoItem(Resource.RptQU_AnsweredCorrect, Resource.RptQU_AnsweredCorrectDesc),
                new ReportInfoItem(Resource.RptQU_AnsweredIncorrect, Resource.RptQU_AnsweredIncorrectDesc),
                new ReportInfoItem(Resource.RptQU_NotAttempted, Resource.RptQU_NotAttemptedDesc),
                new ReportInfoItem(Resource.RptQU_CorrectResponsePct, Resource.RptQU_CorrectResponsePctDesc),
                new ReportInfoItem(Resource.RptQU_IncorrectResponsePct, Resource.RptQU_IncorrectResponsePctDesc),
                new ReportInfoItem(Resource.RptDifficultyLevel, Resource.RptQU_DifficultyLevelDesc),
            ]),
        ];

        [Inject] private IBlazAnalyticalReportsService AnalyticalReportsService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private QuestionUsageReportDto? _report;
        private bool _isLoading;
        private bool _isExporting;
        private string? _errorMessage;
        private int _itemsPage = 1;
        private AnalyticalReportFilterDto _currentFilter = new();
        private const int ItemsPageSize = 100;

        private bool HasReport => _report is not null;
        private bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);
        private bool HasItems => _report?.Items.Count > 0;
        private int TotalPages => _report is null ? 0 : (int)Math.Ceiling(_report.Items.Count / (double)ItemsPageSize);

        private IReadOnlyCollection<QuestionUsageItemDto> CurrentPageItems =>
            _report?.Items
                .Skip((_itemsPage - 1) * ItemsPageSize)
                .Take(ItemsPageSize)
                .ToList() ?? [];


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
                _report = await AnalyticalReportsService
                    .GetQuestionUsageReportAsync(
                        new QuestionUsageReportRequestDto(
                            _currentFilter.ScheduleIds,
                            _currentFilter.VenueCodes,
                            _currentFilter.PaperCodes));

                _itemsPage = 1;
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
                var sheet = BuildExportSheet(_report);

                var (bytes, extension) = ReportExportHelper.Build(Resource.RptTitleQuestionUsage, sheet);

                await JS.InvokeVoidAsync("downloadFileFromBytes", bytes, $"Question_Usage_Report.{extension}");
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

        private static ExportSheet BuildExportSheet(QuestionUsageReportDto report)
        {
            return new ExportSheet(
                Resource.RptTitleQuestionUsage,
                [
                    Resource.RptQuestionCode,
                    Resource.RptNoOfStudents,
                    Resource.RptAnsweredCorrect,
                    Resource.RptAnsweredIncorrect,
                    Resource.RptNotAttempted,
                    Resource.RptCorrectPct,
                    Resource.RptIncorrectPct,
                    Resource.RptDifficultyLevel
                ],
                report.Items.Select(
                    x => (IEnumerable<object?>)
                    [
                        x.QuestionCode,
                        x.NoOfStudents,
                        x.AnsweredCorrect,
                        x.AnsweredIncorrect,
                        x.NotAttempted,
                        x.CorrectResponsePct,
                        x.IncorrectResponsePct,
                        x.DifficultyLevel
                    ]));
        }
    }
}
