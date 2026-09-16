using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.InternalHelperTypes.General;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Reports;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Reports
{
    public partial class ItemAnalysisReport
    {
        [Inject] IBlazAuthService AuthService { get; set; } = default!;

        private static readonly string _infoOverview = Resource.ItemAnalysisReportOverview;

        private static readonly List<ReportInfoSection> _infoSections =
        [
            new ReportInfoSection(Resource.RptSectionFilters,
            [
                new ReportInfoItem(Resource.Paper, Resource.RptIA_FilterPaperDesc),
                new ReportInfoItem(Resource.Form, Resource.RptIA_FilterFormDesc),
                new ReportInfoItem(Resource.RptIA_FilterDateRange, Resource.RptIA_FilterDateRangeDesc),
                new ReportInfoItem(Resource.RptIA_FilterGenderSplit, Resource.RptIA_FilterGenderSplitDesc),
            ]),
            new ReportInfoSection(Resource.RptIA_SectionSheet1,
            [
                new ReportInfoItem(Resource.RptIA_Identifiers, Resource.RptIA_IdentifiersDesc),
                new ReportInfoItem(Resource.RptIA_OpItems, Resource.RptIA_OpItemsDesc),
                new ReportInfoItem(Resource.RptIA_N, Resource.RptIA_NDesc),
                new ReportInfoItem(Resource.RptIA_RawScoreStats, Resource.RptIA_RawScoreStatsDesc),
                new ReportInfoItem(Resource.RptIA_ScaledScoreStats, Resource.RptIA_ScaledScoreStatsDesc),
                new ReportInfoItem(Resource.RptIA_TestTimeStats, Resource.RptIA_TestTimeStatsDesc),
                new ReportInfoItem(Resource.RptIA_CronbachAlpha, Resource.RptIA_CronbachAlphaDesc),
                new ReportInfoItem(Resource.RptIA_Sem, Resource.RptIA_SemDesc),
                new ReportInfoItem(Resource.RptIA_MeanPValue, Resource.RptIA_MeanPValueDesc),
                new ReportInfoItem(Resource.RptIA_MeanItemTotalCorr, Resource.RptIA_MeanItemTotalCorrDesc),
            ]),
            new ReportInfoSection(Resource.RptIA_SectionSheet23,
            [
                new ReportInfoItem(Resource.RptIA_ItemCode, Resource.RptIA_ItemCodeDesc),
                new ReportInfoItem(Resource.Status, Resource.RptIA_StatusDesc),
                new ReportInfoItem(Resource.Key, Resource.RptIA_KeyDesc),
                new ReportInfoItem(Resource.RptIA_n, Resource.RptIA_nDesc),
                new ReportInfoItem(Resource.RptIA_PValue, Resource.RptIA_PValueDesc),
                new ReportInfoItem(Resource.RptIA_PValueVar, Resource.RptIA_PValueVarDesc),
                new ReportInfoItem(Resource.RptIA_ItemTotalCorr, Resource.RptIA_ItemTotalCorrDesc),
                new ReportInfoItem(Resource.RptIA_ResponseTimeStats, Resource.RptIA_ResponseTimeStatsDesc),
                new ReportInfoItem(Resource.RptIA_ItemVsAll, Resource.RptIA_ItemVsAllDesc),
            ]),
            new ReportInfoSection(Resource.RptIA_SectionSheet45,
            [
                new ReportInfoItem(Resource.RptIA_ResponseIdentifiers, Resource.RptIA_ResponseIdentifiersDesc),
                new ReportInfoItem(Resource.RptIA_Metrics, Resource.RptIA_MetricsDesc),
                new ReportInfoItem(Resource.RptIA_MetricBlank, Resource.RptIA_MetricBlankDesc),
                new ReportInfoItem(Resource.RptIA_OptCorr, Resource.RptIA_OptCorrDesc),
                new ReportInfoItem(Resource.RptIA_OptCorrBlank, Resource.RptIA_OptCorrBlankDesc),
                new ReportInfoItem(Resource.RptIA_ResponseVsAll, Resource.RptIA_ResponseVsAllDesc),
            ]),
        ];

        [Inject] private IBlazResultsReportsService ReportService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private List<PaperLookupDto> _papers = [];
        private List<FormLookupDto> _forms = [];
        private PaperLookupDto? _selectedPaper;
        private IEnumerable<FormLookupDto> _selectedForms = [];
        private DateTime? _startDate;
        private DateTime? _endDate;
        private bool _splitByGender = true;
        private bool _isGenerating;
        private string? _errorMessage;
        private string? _noDataMessage;

        public Func<PaperLookupDto, string> PaperToString { get; } = p => p == null ? string.Empty : $"{p.Code} - {p.Name}";
        public Func<FormLookupDto, string> FormToString { get; } = f => f == null ? string.Empty : $"{f.Code} - {f.Name}";


        protected override async Task OnInitializedAsync()
        {
            _papers = await ReportService.GetItemAnalysisPapersAsync();
        }

        private async Task<IEnumerable<PaperLookupDto>> SearchPapersAsync(string value, CancellationToken cancellationToken)
        {
            await Task.Delay(200, cancellationToken);

            if (string.IsNullOrEmpty(value))
            {
                return _papers;
            }

            return _papers.Where(p =>
                (p.Code?.Contains(value, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                (p.Name?.Contains(value, StringComparison.InvariantCultureIgnoreCase) ?? false));
        }

        private async Task OnPaperChangedAsync(PaperLookupDto? paper)
        {
            _selectedPaper = paper;
            _selectedForms = [];
            _forms = [];

            if (paper is not null)
            {
                _forms = await ReportService.GetItemAnalysisFormsAsync(paper.Code);
            }

            await InvokeAsync(StateHasChanged);
        }

        private Task OnFormsChangedAsync(IEnumerable<FormLookupDto> forms)
        {
            _selectedForms = forms;

            return Task.CompletedTask;
        }

        private async Task GenerateReportAsync()
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.ReportGenerator);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            if (_selectedPaper is null || !_selectedForms.Any())
            {
                Snackbar.Add(Resource.RptSelectPaperAndForm, Severity.Warning);

                return;
            }

            _isGenerating = true;
            _errorMessage = null;
            _noDataMessage = null;

            try
            {
                var request = new ItemAnalysisReportRequestDto(
                    _selectedPaper.Id,
                    [.. _selectedForms.Select(x => x.Id)],
                    _splitByGender,
                    1,
                    0,
                    new PaginationSearchModel
                    {
                        PaginationOff = true,
                        FromDate = _startDate,
                        ToDate = _endDate
                    });

                var file = await ReportService.GetItemAnalysisReportAsync(request);

                if (file.Bytes.Length == 0)
                {
                    _noDataMessage = Resource.RptNoCompletedSessions;
                    return;
                }

                await JS.InvokeVoidAsync(
                    "downloadFileFromBytes",
                    file.Bytes,
                    file.FileName);
            }
            catch (Exception ex)
            {
                var message = ex.Message;

                if (message.StartsWith(Resource.RptNoCompletedSessions, StringComparison.Ordinal) || message.StartsWith(Resource.RptNoResponsesFound, StringComparison.Ordinal))
                {
                    _noDataMessage = message;
                }
                else
                {
                    _errorMessage = message;
                }
            }
            finally
            {
                _isGenerating = false;

                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
