using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Services.Interfaces.EquationTemplate;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Dialogs.EquationTemplate.EquationTemplateViewDialog
{
    public partial class EquationTemplateViewDialog
    {
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] private IBlazEquationTemplateService BlazEquationTemplateService { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; }

        [Inject] private IJSRuntime JS { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public GetEquationTemplateResponseDto Model { get; set; } = new GetEquationTemplateResponseDto();

        private bool _isLoading = true;
        private bool _showResults = false;
        private bool _showDetailedResults = false;
        private bool _loadingResults = false;

        private CandidateEquationResultDto FullResults { get; set; }
        private List<CandidateQuestionsAnswersDto> Results { get; set; } = [];
        private Dictionary<string, CategoryCalculationDto> CategoryCalculations => FullResults?.CategoryCalculations ?? [];
        private Dictionary<long, List<ItemBankMetricsDto>> CandidateItemBankMetrics => FullResults?.CandidateItemBankMetrics ?? [];
        private decimal FinalScore => FullResults?.FinalScore ?? 0;
        private string TemplateEquation => FullResults?.TemplateEquation ?? string.Empty;
        private string ProcessedTemplateEquation => FullResults?.ProcessedTemplateEquation ?? string.Empty;
        private TableGroupDefinition<CandidateQuestionsAnswersDto> GroupDefinition { get; set; } = new()
        {
            GroupName = "Candidate",
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = true,
            Selector = (e) => $"{e.FirstName} ({e.CandidateCode})"
        };

        protected override async Task OnInitializedAsync()
        {
            var equationTemplateId = await BlazSessionStorageService.GetValue<long>(MiscConstants.PerformViewBtnClick);

            await LoadEquationTemplateAsync(equationTemplateId);
        }

        private async Task LoadEquationTemplateAsync(long id)
        {
            _isLoading = true;

            StateHasChanged();

            var response = await BlazEquationTemplateService.GetEquationTemplateById(id);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Model = response.Data as GetEquationTemplateResponseDto;
            }
            else
            {
                Snackbar.Add(response.Message ?? Resource.FailedToLoadEquationTemplate, Severity.Error);
            }

            _isLoading = false;
            StateHasChanged();
        }

        private async Task ToggleResults()
        {
            _showResults = !_showResults;

            if (_showResults && (Results == null || Results.Count == 0))
            {
                await LoadResultsAsync();
            }

            StateHasChanged();
        }

        private void ToggleDetailedResults()
        {
            _showDetailedResults = !_showDetailedResults;
            StateHasChanged();
        }

        private async Task LoadResultsAsync()
        {
            if (Model == null)
            {
                Snackbar.Add(Resource.EquationTemplateNotLoaded, Severity.Warning);
                return;
            }

            _loadingResults = true;
            StateHasChanged();

            try
            {
                long formId = long.Parse(Model.FormId.ToString());

                var response = await BlazEquationTemplateService.GetCandidateQuestionsWithEquation(formId);

                if (response.StatusCode == HttpStatusCode.OK && response.Data != null)
                {
                    FullResults = response.Data as CandidateEquationResultDto;

                    Results = FullResults?.Questions ?? [];

                    if (Results.Count == 0)
                    {
                        Snackbar.Add(@Resource.NoResultsFoundForThisEquationTemplate, Severity.Info);
                    }
                    else
                    {
                        Snackbar.Add(Resource.EquationTemplateResultsLoadedSuccessfully, Severity.Success);
                    }
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    FullResults = null;
                    Results = [];
                }
                else
                {
                    FullResults = null;
                    Results = [];
                    Snackbar.Add(response.Message ?? Resource.FailedToloadEquationResults, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                FullResults = null;
                Results = [];
                Snackbar.Add(string.Format(Resource.ErrorLoadingResults, ex.Message), Severity.Error);
            }
            finally
            {
                _loadingResults = false;
                StateHasChanged();
            }
        }

        //private async Task ExportCandidatesAsync()
        //{
        //    if (Model == null)
        //    {
        //        Snackbar.Add(Resource.EquationTemplateNotLoaded, Severity.Warning);
        //        return;
        //    }

        //    _isExporting = true;
        //    StateHasChanged();

        //    try
        //    {
        //        long formId = long.Parse(Model.FormId.ToString());

        //        var response = await BlazEquationTemplateService.ExportCandidatesData(formId);

        //        if (response.StatusCode == HttpStatusCode.OK && response.Data != null)
        //        {
        //            ExportCandidatesResponse exportData = null;

        //            try
        //            {
        //                // 1. Convert the data object to a JSON string safely using .ToString() on a JsonElement or JObject returns the JSON string representation
        //                var jsonString = response.Data.ToString();

        //                // 2. Deserialize directly to the DTO
        //                exportData = JsonSerializer.Deserialize<ExportCandidatesResponse>(
        //                    jsonString,
        //                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        //                );
        //            }
        //            catch
        //            {
        //                // Fallback if .ToString() didn't produce JSON
        //                var json = JsonSerializer.Serialize(response.Data);

        //                exportData = JsonSerializer.Deserialize<ExportCandidatesResponse>(
        //                    json,
        //                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        //                );
        //            }

        //            if (exportData != null && !string.IsNullOrEmpty(exportData.FileName))
        //            {
        //                await DownloadFileAsync(exportData.FileName, exportData.FileContent);

        //                Snackbar.Add(string.Format(Resource.SuccessfullyExported, exportData.CandidateCount), Severity.Success);
        //            }
        //            else
        //            {
        //                Snackbar.Add(Resource.InvalidExportDataStructureReceived, Severity.Error);
        //            }
        //        }
        //        else if (response.StatusCode == HttpStatusCode.NotFound)
        //        {
        //            Snackbar.Add(response.Message ?? Resource.NoCandidatesFoundForThisForm, Severity.Warning);
        //        }
        //        else
        //        {
        //            Snackbar.Add(response.Message ?? Resource.AnErrorOccurredWhileExportingCandidates, Severity.Error);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Snackbar.Add(string.Format(Resource.ErrorExportingCandidates, ex.Message), Severity.Error);
        //    }
        //    finally
        //    {
        //        _isExporting = false;
        //        StateHasChanged();
        //    }
        //}

        private async Task DownloadFileAsync(string fileName, string base64Content)
        {
            try
            {
                await JS.InvokeVoidAsync("downloadBase64File", fileName, base64Content);
            }
            catch (Exception ex)
            {
                Snackbar.Add(string.Format(Resource.ErrorDownloadingFile, ex.Message), Severity.Error);
            }
        }

        private void Close()
        {
            MudDialog.Close();
        }
    }
}