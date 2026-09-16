using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Candidate;
using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Candidate.Responses;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Dialogs.Candidate
{
    public partial class CandidatesImportDialog
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IBlazCandidateService BlazCandidateService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;

        private CandidateAndLookUpsRequestDto candidateAndLookUpsRequestDto = new();
        private JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private MemoryStream stream = new();
        private IBrowserFile normalExcelFile;
        private bool isProcessingFile = false;
        private const string defaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full";
        private string _dragClass = defaultDragClass;
        private readonly List<string> _fileNames = [];
        private MudFileUpload<IBrowserFile> _fileUpload;

        private void OnInputFileChanged(InputFileChangeEventArgs e)
        {
            ClearDragClass();
            _fileNames.Clear();
            var file = e.File;
            HandleFileSelected(file);
            _fileNames.Add(file.Name);
        }

        private void SetDragClass()
            => _dragClass = $"{defaultDragClass} mud-border-primary";

        private void ClearDragClass()
            => _dragClass = defaultDragClass;

        private void HandleFileSelected(IBrowserFile file)
        {
            if (file == null) return;
            normalExcelFile = file;
        }

        private async Task SubmitSelection()
        {
            if (!ValidateCandidatesImportingForm()) return;
            isProcessingFile = true;
            StateHasChanged();

            try
            {
                candidateAndLookUpsRequestDto.File = normalExcelFile;

                var apiResponse = await BlazCandidateService.AddMultipleCandidateAsync(candidateAndLookUpsRequestDto);

                if (apiResponse.StatusCode != HttpStatusCode.OK)
                {
                    var candidateCombinedErrorsResponse = new CandidateCombinedErrorsResponseDto();

                    if (apiResponse.Data is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        candidateCombinedErrorsResponse.ExcelErrorsDto = JsonSerializer.Deserialize<List<ExcelValidationErrorDto>>(
                            jsonElement.GetRawText(),
                            _jsonSerializerOptions
                        );
                    }

                    var parameters = new DialogParameters<CandidateImportErrorsViewDialog>
                    {
                        { x => x.CandidateCombinedErrorsResponseDto, candidateCombinedErrorsResponse },
                    };

                    var options = new DialogOptions() { MaxWidth = MaxWidth.Large, FullWidth = true };

                    await DialogService.ShowAsync<CandidateImportErrorsViewDialog>(string.Empty, parameters, options);
                }
                else
                {
                    Snackbar.Add(apiResponse.Message, Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
            }
            catch (JSException)
            {
                Snackbar.Add(Resource.OopsItLooksLikeTheFileHasExpired, Severity.Warning);
                normalExcelFile = null;
            }
            finally
            {
                isProcessingFile = false;
                StateHasChanged();
            }
        }

        private void DeleteFile()
        {
            normalExcelFile = null;
            _fileUpload.ClearAsync();
            _fileNames.Clear();
            ClearDragClass();
            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            StateHasChanged();
        }

        private bool ValidateCandidatesImportingForm()
        {
            if (normalExcelFile == null)
            {
                Snackbar.Add(Resource.PleaseSlectFileFirst, Severity.Error);
                return false;
            }

            return true;
        }

        private void Cancel() => MudDialog.Cancel();
    }
}