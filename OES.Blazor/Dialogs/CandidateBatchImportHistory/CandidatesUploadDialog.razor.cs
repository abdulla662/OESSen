using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Dialogs.Candidate;
using OES.Blazor.Services.Interfaces.Candidate;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Candidate.Responses;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Dialogs.CandidateBatchImportHistory
{
    public partial class CandidatesUploadDialog
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IBlazCandidateService BlazCandidateService { get; set; } = default!;
        [Inject] private IBlazVenueService BlazVenueService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public long SchedulePaperId { get; set; }
        [Parameter] public List<long> ScheduleVenuesIds { get; set; } = [];

        private List<GetVenueResponseDto> AllAvailableVenues { get; set; } = [];
        private List<GetVenueResponseDto> ScheduleVenues { get; set; } = [];
        private CandidateDataCompositeRequestDto ValidationRequestDto { get; set; } = new();
        private CandidateAndLookUpsAndSchedulePapersRequestDto RequestDto { get; set; } = new();

        private readonly List<string> _fileNames = [];
        private IBrowserFile normalExcelFile;
        private GetVenueResponseDto _selectedVenue;
        private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private bool isProcessingFile = false;
        private const string defaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full";
        private string _dragClass = defaultDragClass;
        private bool isValid = false;

        protected override async Task OnInitializedAsync()
        {
            isValid = false;

            AllAvailableVenues = await BlazVenueService.GetAllVenuesAsync();

            if (ScheduleVenuesIds is { Count: > 0 })
                ScheduleVenues = [.. AllAvailableVenues.Where(x => ScheduleVenuesIds.Contains(x.Id))];
            else
                ScheduleVenues = AllAvailableVenues;
        }

        private void OnInputFileChanged(InputFileChangeEventArgs e)
        {
            ClearDragClass();
            _fileNames.Clear();
            var file = e.File;
            normalExcelFile = file;
            _fileNames.Add(file.Name);
        }

        private void SetDragClass() => _dragClass = $"{defaultDragClass} mud-border-primary";

        private void ClearDragClass() => _dragClass = defaultDragClass;

        private async Task SubmitAndValidateSelection()
        {
            try
            {
                if (!Validate()) return;

                isProcessingFile = true;

                ValidationRequestDto.SchedulePaperId = SchedulePaperId;
                ValidationRequestDto.File = normalExcelFile;
                ValidationRequestDto.VenueCode = _selectedVenue.Code;

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
                RequestDto.File = normalExcelFile;
                RequestDto.VenueId = _selectedVenue.Id;

                var res = await BlazCandidateService.AddMultipleCandidateAndSchedulePaperAsync(RequestDto);

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
            normalExcelFile = null;
            _fileNames.Clear();
            isValid = false;
            StateHasChanged();
        }

        private bool Validate()
        {
            if (SchedulePaperId <= 0)
            {
                Snackbar.Add(Resource.InvalidSchedeulePaperId, Severity.Error);
                return false;
            }
            if (normalExcelFile == null)
            {
                Snackbar.Add(Resource.PleaseSlectFileFirst, Severity.Error);
                return false;
            }
            if (_selectedVenue == null || _selectedVenue.Id == 0)
            {
                Snackbar.Add(Resource.SelectVenueForCandidates, Severity.Error);
                return false;
            }
            return true;
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
