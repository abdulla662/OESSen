using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Services;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;

namespace OES.Blazor.Pages.Reports
{
    public partial class QuestionBlockActivityReport
    {
        [Inject] private IBlazAnalyticalReportsService AnalyticalReportsService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private static readonly QuestionBlockActivityReportDto _emptyReport = new(null, []);
        private AdaptivePaperSubtype? _examType;
        private QuestionBlockActivityReportDto _report = _emptyReport;
        private bool _isLoading;
        private bool _isExporting;
        private string _errorMessage;

        private bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);
        private bool HasReport => _report.Rows.Count > 0;
        private string ExamTypeLabel => _report.ExamType switch
        {
            AdaptivePaperSubtype.MST => nameof(AdaptivePaperSubtype.MST),
            AdaptivePaperSubtype.STEP => nameof(AdaptivePaperSubtype.STEP),
            _ => Resource.All
        };

        private async Task GenerateAsync() => await LoadReportAsync();

        private async Task LoadReportAsync()
        {
            ResetError();

            ResetReport();

            _isLoading = true;

            await InvokeAsync(StateHasChanged);

            try
            {
                _report = await AnalyticalReportsService.GetQuestionBlockActivityReportAsync(
                    new QuestionBlockActivityReportRequestDto(_examType));
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
            if (!HasReport)
            {
                return;
            }

            _isExporting = true;

            await InvokeAsync(StateHasChanged);

            try
            {
                var sheet = BuildExportSheet(_report);

                var (bytes, extension) = ReportExportHelper.Build(Resource.RptTitleQuestionBlockActivity, sheet);

                await JS.InvokeVoidAsync("downloadFileFromBytes", bytes, $"Question_Block_Activity_Report.{extension}");
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
            _report = _emptyReport;
        }

        private static Color GetStatusColor(string status)
        {
            return status == Resource.Active ? Color.Success : Color.Default;
        }

        private static ExportSheet BuildExportSheet(QuestionBlockActivityReportDto report)
        {
            return new ExportSheet(
                Resource.RptTitleQuestionBlockActivity,
                [
                    Resource.QuestionId,
                    Resource.RptQuestionCode,
                    Resource.Block,
                    Resource.BlockCode,
                    Resource.Status
                ],
                report.Rows.Select(
                    row => (IEnumerable<object?>)
                    [
                        row.QuestionId,
                        row.QuestionCode,
                        row.BlockId,
                        row.BlockCode,
                        row.Status
                    ]));
        }

        private static string GetExamTypeLabel(AdaptivePaperSubtype? value)
        {
            return value switch
            {
                AdaptivePaperSubtype.MST => nameof(AdaptivePaperSubtype.MST),
                AdaptivePaperSubtype.STEP => nameof(AdaptivePaperSubtype.STEP),
                _ => Resource.All
            };
        }
    }
}
