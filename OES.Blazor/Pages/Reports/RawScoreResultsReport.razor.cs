using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.InternalHelperTypes.General;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Reports;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Text;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.Reports
{
    public partial class RawScoreResultsReport
    {
        private static readonly string _infoOverview = Resource.RawScoreResultsReportOverview;

        private static readonly List<ReportInfoSection> _infoSections =
        [
            new ReportInfoSection(Resource.RptSectionTableColumns,
            [
                new ReportInfoItem(Resource.RptRS_NationalID, Resource.RptRS_NationalIDDesc),
                new ReportInfoItem(Resource.Name, Resource.RptRS_NameDesc),
                new ReportInfoItem(Resource.Gender, Resource.RptRS_GenderDesc),
                new ReportInfoItem(Resource.Email, Resource.RptRS_EmailDesc),
                new ReportInfoItem(Resource.Form, Resource.RptRS_FormDesc),
                new ReportInfoItem(Resource.RptRS_TotalMarks, Resource.RptRS_TotalMarksDesc),
                new ReportInfoItem(Resource.Score, Resource.RptRS_ScoreDesc),
                new ReportInfoItem(Resource.RptRS_ScorePct, Resource.RptRS_ScorePctDesc),
            ]),
            new ReportInfoSection(Resource.RptSectionInteractions,
            [
                new ReportInfoItem(Resource.RptItemSearch, Resource.RptRS_SearchDesc),
                new ReportInfoItem(Resource.RptRS_ExportCSV, Resource.RptRS_ExportCSVDesc),
            ]),
        ];

        [Inject] private IBlazResultsReportsService ResultsReportsService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private List<PaperLookupDto> _papers = [];
        private List<FormLookupDto> _forms = [];

        private PaperLookupDto? _selectedPaper;
        private IEnumerable<FormLookupDto> _selectedForms = [];
        private MudDataGrid<RawScoreResultRowDto>? _grid;
        private DateTime? _startDate;
        private DateTime? _endDate;

        private bool _isLoading;
        private string? _errorMessage;
        private string? _noDataMessage;
        private string? _searchString;
        private bool _hasGenerated;
        private int _totalCount;
        private string? _paperName = string.Empty;
        private List<string> _formNames = [];
        private CancellationTokenSource _searchDebounce = new();

        public Func<PaperLookupDto, string> PaperToString = p => p == null ? string.Empty : $"{p.Code} - {p.Name}";
        public Func<FormLookupDto, string> FormToString = f => f == null ? string.Empty : $"{f.Code} - {f.Name}";


        protected override async Task OnInitializedAsync()
        {
            _papers = await ResultsReportsService.GetItemAnalysisPapersAsync();
            Console.WriteLine($"Loaded {_papers.Count} papers for item analysis.");
        }

        private async Task<IEnumerable<PaperLookupDto>> SearchPapersAsync(string value, CancellationToken cancellationToken)
        {
            await Task.Delay(200, cancellationToken);

            if (string.IsNullOrWhiteSpace(value))
            {
                return _papers;
            }

            return _papers.Where(x => (x.Code?.Contains(value, StringComparison.InvariantCultureIgnoreCase) ?? false) || (x.Name?.Contains(value, StringComparison.InvariantCultureIgnoreCase) ?? false));
        }

        private async Task OnPaperChangedAsync(PaperLookupDto? paper)
        {
            _selectedPaper = paper;

            ResetForms();

            ResetReportState();

            if (paper != null)
            {
                _forms = await ResultsReportsService.GetItemAnalysisFormsAsync(paper.Code);
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task OnFormsChangedAsync(IEnumerable<FormLookupDto> forms)
        {
            _selectedForms = forms;

            ResetReportState();

            await InvokeAsync(StateHasChanged);
        }

        private async Task GenerateReportAsync()
        {
            if (_selectedPaper == null || !_selectedForms.Any())
            {
                Snackbar.Add(Resource.RptSelectPaperAndForm, Severity.Warning);

                return;
            }

            ResetMessages();

            _searchString = null;

            _hasGenerated = true;

            await InvokeAsync(StateHasChanged);

            await Task.Yield();

            if (_grid is not null)
            {
                await _grid.ReloadServerData();
            }
        }

        private async Task<GridData<RawScoreResultRowDto>> LoadServerDataAsync(GridState<RawScoreResultRowDto> state)
        {
            if (_selectedPaper == null || !_selectedForms.Any())
            {
                return new GridData<RawScoreResultRowDto> { TotalItems = 0, Items = [] };
            }
            _isLoading = true;

            await InvokeAsync(StateHasChanged);

            try
            {
                var request = BuildRequest(state);

                var result = await ResultsReportsService.GetRawScoreResultsReportAsync(request);

                _totalCount = result.TotalCount;
                _paperName = result.PaperName;
                _formNames = result.FormNames;
                _errorMessage = null;
                _noDataMessage = result.TotalCount == 0 ? Resource.RptNoCompletedSessions : null;

                return new GridData<RawScoreResultRowDto>
                {
                    TotalItems = result.TotalCount,
                    Items = result.Rows
                };
            }
            catch (Exception exception)
            {
                HandleLoadError(exception);

                return new GridData<RawScoreResultRowDto> { TotalItems = 0, Items = [] };
            }
            finally
            {
                _isLoading = false;

                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task OnSearchInputAsync(string value)
        {
            _searchString = value;

            _searchDebounce.Cancel();

            _searchDebounce.Dispose();

            _searchDebounce = new CancellationTokenSource();

            try
            {
                await Task.Delay(400, _searchDebounce.Token);

                if (_grid is not null)
                {
                    await _grid.ReloadServerData();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task ExportCsvAsync()
        {
            if (!_hasGenerated || _selectedPaper is null || !_selectedForms.Any())
            {
                return;
            }

            _isLoading = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                var request = BuildExportRequest();
                var result = await ResultsReportsService.GetRawScoreResultsReportAsync(request);

                if (result.Rows.Count == 0)
                {
                    return;
                }

                var csv = BuildCsv(result);

                var bom = Encoding.UTF8.GetPreamble();
                var body = Encoding.UTF8.GetBytes(csv);
                var bytes = new byte[bom.Length + body.Length];
                bom.CopyTo(bytes, 0);
                body.CopyTo(bytes, bom.Length);

                await JS.InvokeVoidAsync("downloadFileFromBytes", bytes, BuildExportFileName());
            }
            finally
            {
                _isLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private RawScoreResultsReportRequestDto BuildRequest(GridState<RawScoreResultRowDto> state)
        {
            var sort = state.SortDefinitions.FirstOrDefault();

            return new RawScoreResultsReportRequestDto(
                [.. _selectedForms.Select(x => x.Id)],
                new PaginationSearchModel
                {
                    PageIndex = state.Page,
                    PageSize = state.PageSize,
                    SearchKey = _searchString,
                    OrderBy =
                        BuildOrderBy(sort),
                    FromDate = _startDate,
                    ToDate = _endDate
                });
        }

        private RawScoreResultsReportRequestDto BuildExportRequest()
        {
            return new RawScoreResultsReportRequestDto(
                [.. _selectedForms.Select(x => x.Id)],
                new PaginationSearchModel
                {
                    PaginationOff = true,
                    FromDate = _startDate,
                    ToDate = _endDate
                });
        }

        private static string? BuildOrderBy(SortDefinition<RawScoreResultRowDto>? sort)
        {
            if (sort is null)
            {
                return null;
            }

            var suffix = sort.Descending ? "_desc" : string.Empty;

            return $"{sort.SortBy?.ToLowerInvariant()}{suffix}";
        }

        private void HandleLoadError(Exception exception)
        {
            var message = exception.Message;

            if (message.StartsWith(Resource.RptNoCompletedSessions) || message.StartsWith(Resource.RptNoResponsesFound))
            {
                _noDataMessage = message;
            }
            else
            {
                _errorMessage = string.Format(Resource.ReportGenerationFailed, message);
            }
        }

        private string BuildCsv(RawScoreResultsPagedDto result)
        {
            var builder = new StringBuilder();

            builder.AppendLine(Resource.RptTitleRawScoreResults);
            builder.AppendLine($"{Resource.RptPaperLabel},{EscapeCsv(result.PaperName)}");
            builder.AppendLine($"{Resource.Form},{EscapeCsv(string.Join(" | ", result.FormNames))}");

            if (_startDate.HasValue)
            {
                builder.AppendLine($"{Resource.RptDateFrom},{_startDate:yyyy-MM-dd}");
            }

            if (_endDate.HasValue)
            {
                builder.AppendLine($"{Resource.RptDateTo},{_endDate:yyyy-MM-dd}");
            }

            builder.AppendLine($"{Resource.Row},{result.TotalCount}");

            builder.AppendLine($"{Resource.Generate},{DateTime.UtcNow:yyyy-MM-dd HH:mm}");

            builder.AppendLine();

            builder.AppendJoin(
                ",",
                Resource.NationalId,
                Resource.CandidateName,
                Resource.Gender,
                Resource.Email,
                Resource.RptPaperLabel,
                Resource.Form,
                Resource.RptTotalMarks,
                Resource.Score,
                Resource.RptScorePct).AppendLine();

            foreach (var row in result.Rows)
            {
                builder.AppendJoin(
                    ",",
                    EscapeCsv(row.NationalId),
                    EscapeCsv(row.Name),
                    EscapeCsv(row.Gender),
                    EscapeCsv(row.Email),
                    EscapeCsv(row.PaperName),
                    EscapeCsv(row.FormName),
                    row.TotalMarks,
                    row.Score,
                    row.ScorePct).AppendLine();
            }

            return builder.ToString();
        }

        private string BuildExportFileName()
        {
            var paperPart = Regex.Replace(_selectedPaper?.Code ?? string.Empty, @"[^\w\-.]", "_").Trim('_');

            var dateRange =
                (_startDate, _endDate)
                switch
                {
                    ({ } from, { } to) => $"{from:yyyyMMdd}-{to:yyyyMMdd}",

                    ({ } from, null) => $"from_{from:yyyyMMdd}",

                    (null, { } to) => $"to_{to:yyyyMMdd}",

                    _ => string.Empty
                };

            var parts = new[]
            {
                "Raw_Score_Results",
                dateRange,
                paperPart
            }
            .Where(x => !string.IsNullOrWhiteSpace(x));

            return $"{string.Join("_", parts)}.csv";
        }

        private void ResetForms()
        {
            _forms = [];

            _selectedForms = [];
        }

        private void ResetMessages()
        {
            _errorMessage = null;

            _noDataMessage = null;
        }

        private void ResetReportState()
        {
            _hasGenerated = false;

            _totalCount = 0;

            ResetMessages();
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (value.Length > 10 && value.All(char.IsDigit))
            {
                return $"\"{value}\"";
            }

            return value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }

        private void ClearNoDataMessage()
        {
            _noDataMessage = null;
        }

        private void ClearErrorMessage()
        {
            _errorMessage = null;
        }
    }
}
