using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Dialogs.Candidate;
using OES.Blazor.Services.Interfaces.Candidate;
using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Candidate.Responses;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Dialogs.CandidateBatchImportHistory
{
    public partial class DumpImportCandidatesDialog
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IBlazCandidateService BlazCandidateService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public long SchedulePaperId { get; set; }
        [Parameter] public List<string> ScheduleVenueCodes { get; set; } = [];

        private DumpImportCandidatesWithSchedulePaperRequestDto RequestDto { get; set; } = new();
        private CandidateDataCompositeRequestDto ValidationRequestDto { get; set; } = new();

        private readonly List<string> _fileNames = [];
        private IBrowserFile autoExcelFile;
        private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private const string defaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full";
        private string _dragClass = defaultDragClass;
        private bool isProcessingFile = false;
        private bool isValid = false;

        private void OnInputFileChanged(InputFileChangeEventArgs e)
        {
            ClearDragClass();
            _fileNames.Clear();
            autoExcelFile = e.File;
            _fileNames.Add(autoExcelFile.Name);
        }

        private void SetDragClass() => _dragClass = $"{defaultDragClass} mud-border-primary";

        private void ClearDragClass() => _dragClass = defaultDragClass;

        private async Task SubmitAndValidateSelection()
        {
            try
            {
                isProcessingFile = true;

                if (SchedulePaperId <= 0)
                {
                    Snackbar.Add(Resource.InvalidSchedeulePaperId, Severity.Error);
                    return;
                }
                if (autoExcelFile == null)
                {
                    Snackbar.Add(Resource.PleaseSelectFileForDump, Severity.Error);
                    return;
                }

                ValidationRequestDto.FromDumb = true;
                ValidationRequestDto.SchedulePaperId = SchedulePaperId;
                ValidationRequestDto.File = autoExcelFile;
                ValidationRequestDto.ScheduleVenueCodes = ScheduleVenueCodes;

                var validationResponse = await BlazCandidateService.ValidateCandidatesDataAsync(ValidationRequestDto);

                if (validationResponse.StatusCode != HttpStatusCode.OK)
                {
                    var candidateCombinedErrorsResponseDto = JsonSerializer.Deserialize<CandidateCombinedErrorsResponseDto>(validationResponse.Data.ToString(), jsonSerializerOptions);

                    var parameters = new DialogParameters<CandidateImportErrorsViewDialog>
                    {
                        { x => x.CandidateCombinedErrorsResponseDto, candidateCombinedErrorsResponseDto }
                    };

                    var options = new DialogOptions() { MaxWidth = MaxWidth.Large, FullWidth = true };

                    await DialogService.ShowAsync<CandidateImportErrorsViewDialog>(string.Empty, parameters, options);

                    return;
                }

                isValid = true;
                isProcessingFile = false;
                Snackbar.Add(validationResponse.Message, Severity.Success);
            }
            catch (JSException)
            {
                Snackbar.Add(Resource.OopsItLooksLikeTheFileHasExpired, Severity.Warning);
            }
            finally
            {
                isProcessingFile = false;
            }
        }

        private async Task SubmitAndUploadSelection()
        {
            try
            {
                if (!isValid) return;

                isProcessingFile = true;

                RequestDto.SchedulePaperId = SchedulePaperId;
                RequestDto.File = autoExcelFile;
                RequestDto.ScheduleVenueCodes = ScheduleVenueCodes;

                var res = await BlazCandidateService.DumpImportCandidatesWithSchedulePaperAsync(RequestDto);

                if (res.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(res.Message, Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add(res.Message, Severity.Error);
                }

                isProcessingFile = false;
            }
            catch (JSException)
            {
                Snackbar.Add(Resource.OopsItLooksLikeTheFileHasExpired, Severity.Warning);
            }
            finally
            {
                isProcessingFile = false;
            }
        }

        private void DeleteFile()
        {
            autoExcelFile = null;
            _fileNames.Clear();
            isValid = false;
            StateHasChanged();
        }

        private async Task DownloadCandidatesWithVenuesTemplateAsync()
        {
            var apiResponse = await BlazCandidateService.DownloadCandidatesWithExistingVenuesExcelFileAsync();

            var candidateTemplateDownloadDto = (CandidateTemplateDownloadDto)apiResponse.Data ?? new();

            if (!string.IsNullOrWhiteSpace(candidateTemplateDownloadDto?.CandidatesTemplateFileUrl))
            {
                string fileName = candidateTemplateDownloadDto.FileName ?? "ImportCandidateWithVenueCode.xlsx";

                string fullPath = string.Format(
                    "{0}{1}",
                    CentralizedUrlHelper.OesApiBaseUrl.TrimEnd('/'),
                    candidateTemplateDownloadDto.CandidatesTemplateFileUrl
                );

                await JS.InvokeVoidAsync("downloadFileFromUrl", fullPath, fileName);
            }
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
