using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Candidate;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Net;

namespace OES.Blazor.Dialogs.Candidate
{
    public partial class CandidatesImportWithinSchedulePaperDialog
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IBlazCandidateService BlazCandidateService { get; set; } = default!;
        [Inject] private IBlazVenueService BlazVenueService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public long SchedulePaperId { get; set; }
        [Parameter] public List<string> ScheduleVenueCodes { get; set; } = [];

        private List<GetVenueResponseDto> AllAvailableVenues = [];
        private List<GetVenueResponseDto> ScheduleVenues = [];
        private CandidateAndLookUpsAndSchedulePapersRequestDto candidateAndLookUpsAndSchedulePapersRequestDto = new();
        private DumpImportCandidatesWithSchedulePaperRequestDto dumpImportCandidatesWithSchedulePaperReuqestDto = new();
        private MudFileUpload<IBrowserFile> normalExcelFileUploaderRef;
        private MudFileUpload<IBrowserFile> autoExcelFileUploaderRef;

        private MemoryStream stream = new();
        private IBrowserFile normalExcelFile;
        private IBrowserFile autoExcelFile;
        private bool isProcessingFile = false;
        private GetVenueResponseDto _selectedVenue;
        private int _activeTabIndex = 0;

        protected override async Task OnInitializedAsync()
        {
            AllAvailableVenues = await BlazVenueService.GetAllVenuesAsync();

            ScheduleVenues = [.. AllAvailableVenues.Where(x => ScheduleVenueCodes.Contains(x.Code))];
        }

        private void OnTabChanged(int index)
        {
            _activeTabIndex = index;
            StateHasChanged();
        }

        private void HandleFileSelected(IBrowserFile file)
        {
            if (file == null) return;

            if (_activeTabIndex == 0)
            {
                normalExcelFile = file;
            }
            else if (_activeTabIndex == 1)
            {
                autoExcelFile = file;
            }
            else
            {
                Snackbar.Add(Resource.OnlyOneFileAtATime, Severity.Error);
            }
        }

        private async Task SubmitSelection()
        {
            if (!ValidateCandidatesImportingForm()) return;
            isProcessingFile = true;
            StateHasChanged();

            candidateAndLookUpsAndSchedulePapersRequestDto.File = normalExcelFile;
            candidateAndLookUpsAndSchedulePapersRequestDto.SchedulePaperId = SchedulePaperId;
            candidateAndLookUpsAndSchedulePapersRequestDto.VenueId = _selectedVenue.Id;

            var res = await BlazCandidateService.AddMultipleCandidateAndSchedulePaperAsync(candidateAndLookUpsAndSchedulePapersRequestDto);

            if (res.StatusCode != HttpStatusCode.OK)
            {
                Snackbar.Add(res.Message, Severity.Error);
                isProcessingFile = false;
            }
            else
            {
                Snackbar.Add(res.Message, Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
        }

        private async Task SubmitDumpImport()
        {
            if (autoExcelFile == null)
            {
                Snackbar.Add(Resource.PleaseSelectFileForDumpImportFirst, Severity.Error);
                return;
            }

            isProcessingFile = true;
            StateHasChanged();

            dumpImportCandidatesWithSchedulePaperReuqestDto.SchedulePaperId = SchedulePaperId;
            dumpImportCandidatesWithSchedulePaperReuqestDto.File = autoExcelFile;
            dumpImportCandidatesWithSchedulePaperReuqestDto.ScheduleVenueCodes = ScheduleVenueCodes;

            var apiResponse = await BlazCandidateService.DumpImportCandidatesWithSchedulePaperAsync(dumpImportCandidatesWithSchedulePaperReuqestDto);

            if (apiResponse.StatusCode != HttpStatusCode.OK)
            {
                Snackbar.Add(apiResponse.Message, Severity.Error);
            }
            else
            {
                Snackbar.Add(apiResponse.Message, Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            StateHasChanged();
        }

        private void DeleteFile()
        {
            if (_activeTabIndex == 0)
            {
                normalExcelFile = null;
                normalExcelFileUploaderRef.ClearAsync();
            }
            else if (_activeTabIndex == 1)
            {
                autoExcelFile = null;
                autoExcelFileUploaderRef.ClearAsync();
            }

            stream.Position = 0;

            stream.Seek(0, SeekOrigin.Begin);

            StateHasChanged();
        }

        private bool ValidateCandidatesImportingForm()
        {
            if (SchedulePaperId == 0)
            {
                Snackbar.Add(Resource.InvalidSchedulePaperId, Severity.Error);
                return false;
            }

            if (normalExcelFile == null)
            {
                Snackbar.Add(Resource.PleaseSelectAFileFirst, Severity.Error);
                return false;
            }

            if (_selectedVenue == null || _selectedVenue.Id == 0)
            {
                Snackbar.Add(Resource.PleaseSelectAVenue, Severity.Error);
                return false;
            }

            return true;
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