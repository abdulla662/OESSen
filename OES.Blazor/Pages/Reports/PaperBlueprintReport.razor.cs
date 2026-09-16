using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OES.Blazor.InternalHelperTypes.General;
using OES.Blazor.Services;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Reports
{
    public partial class PaperBlueprintReport
    {
        private static readonly string _infoOverview = Resource.PaperBlueprintReportOverview;

        private static readonly List<ReportInfoSection> _infoSections =
        [
            new ReportInfoSection(Resource.RptSectionBarChart,
            [
                new ReportInfoItem(Resource.RptItemHorizontalBars, Resource.RptPB_BarsDesc),
                new ReportInfoItem(Resource.RptItemCountLabel, Resource.RptPB_CountLabelDesc),
            ]),
            new ReportInfoSection(Resource.RptSectionTable,
            [
                new ReportInfoItem(Resource.RptItemRows, Resource.RptPB_RowsDesc),
                new ReportInfoItem(Resource.RptItemColumns, Resource.RptPB_ColumnsDesc),
                new ReportInfoItem(Resource.RptItemCells, Resource.RptPB_CellsDesc),
            ]),
        ];

        [Inject] private IBlazAnalyticalReportsService AnalyticalReportsService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private PaperBlueprintReportDto? _report;
        private bool _isLoading;
        private bool _isExporting;
        private string? _errorMessage;
        private AnalyticalReportFilterDto _currentFilter = new();
        private IReadOnlyDictionary<string, int> _typeTotals = new Dictionary<string, int>();

        private bool HasReport => _report is not null;
        private bool HasRows => _report?.Rows.Count > 0;
        private int MaxTypeTotal => Math.Max(1, _typeTotals.Values.DefaultIfEmpty(1).Max());


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
                _report = await AnalyticalReportsService.GetPaperBlueprintReportAsync(
                    new PaperBlueprintReportRequestDto(
                        _currentFilter.ScheduleIds,
                        _currentFilter.PaperCodes,
                        _currentFilter.FormIds));

                _typeTotals = BuildTypeTotals(_report);
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

                var (bytes, extension) = ReportExportHelper.Build(Resource.RptTitlePaperBlueprint, sheet);

                await JS.InvokeVoidAsync("downloadFileFromBytes", bytes, $"Paper_Blueprint_Report.{extension}");
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

            _typeTotals = new Dictionary<string, int>();
        }

        private int GetTypeTotal(string itemType)
        {
            return _typeTotals.GetValueOrDefault(itemType);
        }

        private static int GetTypeCount(ItemTypeBankRowDto row, string itemType)
        {
            return row.CountsByType.GetValueOrDefault(itemType);
        }

        private string BuildBarWidthStyle(string itemType)
        {
            var count = GetTypeTotal(itemType);
            var widthPercentage = (double)count / MaxTypeTotal * 100;

            return $"width:{widthPercentage:F1}%";
        }

        private static IReadOnlyDictionary<string, int> BuildTypeTotals(PaperBlueprintReportDto report)
        {
            return report.ItemTypes.ToDictionary(
                    itemType => itemType,
                    itemType => report.Rows.Sum(row => row.CountsByType.GetValueOrDefault(itemType)));
        }

        private static ExportSheet BuildExportSheet(PaperBlueprintReportDto report)
        {
            var headers =
                new[]
                {
                    Resource.RptItemBankLabel
                }
                .Concat(report.ItemTypes)
                .ToArray();

            var rows = report.Rows.Select(
                row => (IEnumerable<object?>)
                    [
                        row.ItemBankName,
                        .. report.ItemTypes.Select(itemType => (object?)row.CountsByType.GetValueOrDefault(itemType)),
                    ]);

            return new ExportSheet(Resource.RptTitlePaperBlueprint, headers, rows);
        }
    }
}
