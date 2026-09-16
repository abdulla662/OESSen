using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.InternalHelperTypes.General;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Reports
{
    public partial class ResponseMatrixReport
    {
        private static readonly string _infoOverview = Resource.ResponseMatrixReportOverview;

        private static readonly List<ReportInfoSection> _infoSections =
        [
            new ReportInfoSection(Resource.RptRM_SectionDownloadedFile,
            [
                new ReportInfoItem(Resource.RptItemRows, Resource.RptRM_RowsDesc),
                new ReportInfoItem(Resource.RptItemColumns, Resource.RptRM_ColumnsDesc),
                new ReportInfoItem(Resource.RptRM_CellValues, Resource.RptRM_CellValuesDesc),
                new ReportInfoItem(Resource.RptRM_FileName, Resource.RptRM_FileNameDesc),
            ]),
        ];

        [Inject] private IBlazResultsReportsService ResultsReportsService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private AnalyticalReportFilterDto _currentFilter = new();
        private bool _isGenerating;
        private string? _errorMessage;
        private string? _noDataMessage;


        private async Task OnFilterChangedAsync(AnalyticalReportFilterDto filter)
        {
            _currentFilter = filter;

            ResetMessages();

            await Task.CompletedTask;
        }

        private async Task GenerateAsync()
        {
            if (!_currentFilter.FormIds.Any())
            {
                Snackbar.Add(Resource.InvalidPaperFormId, Severity.Warning);

                return;
            }

            _isGenerating = true;

            ResetMessages();

            try
            {
                var request = BuildRequest();

                var file = await ResultsReportsService.GetResponseMatrixReportAsync(request);

                if (file.Bytes.Length == 0)
                {
                    _noDataMessage = Resource.RptNoResponsesFound;

                    return;
                }

                await JS.InvokeVoidAsync("downloadFileFromBytes", file.Bytes, file.FileName);
            }
            catch (Exception ex)
            {
                _errorMessage = string.Format(Resource.ReportGenerationFailed, ex.Message);
            }
            finally
            {
                _isGenerating = false;
            }
        }

        private ResponseMatrixReportRequestDto BuildRequest()
        {
            return new(
                _currentFilter.FormIds,
                new PaginationSearchModel
                {
                    PaginationOff = true,
                    FromDate = _currentFilter.DateFrom,
                    ToDate = _currentFilter.DateTo
                });
        }

        private void ResetMessages()
        {
            _errorMessage = null;

            _noDataMessage = null;
        }

        private void ClearErrorMessage()
        {
            _errorMessage = null;
        }

        private void ClearNoDataMessage()
        {
            _noDataMessage = null;
        }
    }
}
