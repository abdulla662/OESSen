using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Dialogs.Reports.ItemBankReportSelectionDialog;
using OES.Blazor.InternalHelperTypes.General;
using OES.Blazor.Services;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.ResourceFiles;


namespace OES.Blazor.Pages.Reports
{
    public partial class ItemTypeReport
    {
        private static readonly string _infoOverview = Resource.ItemTypeReportOverview;

        private static readonly List<ReportInfoSection> _infoSections =
        [
            new ReportInfoSection(Resource.RptSectionBarChart,
            [
                new ReportInfoItem(Resource.RptItemHorizontalBars, Resource.RptIT_BarsDesc),
                new ReportInfoItem(Resource.RptItemCountLabel, Resource.RptIT_CountLabelDesc),
            ]),
            new ReportInfoSection(Resource.RptSectionTable,
            [
                new ReportInfoItem(Resource.RptItemRows, Resource.RptIT_RowsDesc),
                new ReportInfoItem(Resource.RptItemColumns, Resource.RptIT_ColumnsDesc),
                new ReportInfoItem(Resource.RptItemCells, Resource.RptIT_CellsDesc),
            ]),
        ];

        [Inject] private IBlazAnalyticalReportsService AnalyticalReportsService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private ItemBankLookupDto? _selectedBank;
        private ItemTypeReportDto? _report;
        private bool _isLoading;
        private bool _isExporting;
        private string? _errorMessage;

        private bool HasReport => _report is not null;
        private IReadOnlyDictionary<string, int> TypeTotals => BuildTypeTotals(_report);
        private int MaxTypeTotal => Math.Max(1, TypeTotals.Values.DefaultIfEmpty(1).Max());
        private static string LocalizeType(string type) => type.ToLocalizedString<Helper.Enums.QuestionType>();


        private async Task OpenItemBankTreeAsync()
        {
            var dialog = await DialogService.ShowAsync<ItemBankReportSelectionDialog>(
                Resource.RptSelectItemBank,
                new DialogOptions
                {
                    MaxWidth = MaxWidth.Medium,
                    FullWidth = true,
                    CloseButton = true
                });

            var result = await dialog.Result;

            if (result.Canceled)
            {
                return;
            }

            if (result.Data is not ItemBankLookupDto selectedBank)
            {
                return;
            }

            _selectedBank = selectedBank;

            ResetReport();
        }

        private async Task GenerateAsync()
        {
            await LoadReportAsync(_selectedBank?.Id);
        }

        private async Task LoadReportAsync(long? itemBankId)
        {
            ResetError();

            _isLoading = true;

            await InvokeAsync(StateHasChanged);

            try
            {
                _report = await AnalyticalReportsService.GetItemTypeReportAsync(itemBankId);

                _report ??= new ItemTypeReportDto();
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

                var (bytes, extension) = ReportExportHelper.Build(Resource.RptTitleItemType, sheet);

                await JS.InvokeVoidAsync("downloadFileFromBytes", bytes, $"Item_Type_Report.{extension}");
            }
            finally
            {
                _isExporting = false;

                await InvokeAsync(StateHasChanged);
            }
        }

        private static IReadOnlyDictionary<string, int> BuildTypeTotals(ItemTypeReportDto? report)
        {
            if (report is null)
            {
                return new Dictionary<string, int>();
            }

            return report.ItemTypes.ToDictionary(
                itemType => itemType,
                itemType =>
                    report.Rows.Sum(
                        row =>
                            row.CountsByType
                                .GetValueOrDefault(
                                    itemType)));
        }

        private static ExportSheet BuildExportSheet(ItemTypeReportDto report)
        {
            var headers = new[]
            {
                Resource.RptItemBankLabel
            }.Concat(report.ItemTypes).ToArray();

            var rows = report.Rows.Select(row => (IEnumerable<object?>)
                [
                    row.ItemBankName,
                    .. report.ItemTypes.Select(
                        itemType =>
                            (object?)row.CountsByType
                                .GetValueOrDefault(
                                    itemType)),
                ]);

            return new ExportSheet(Resource.RptTitleItemType, headers, rows);
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

        private Task ClearBankAsync()
        {
            _selectedBank = null;
            ResetReport();
            return Task.CompletedTask;
        }
    }
}
