using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.OrganizationStructure;
using OES.Blazor.Services.Interfaces.Candidate;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Dialogs.Candidate
{
    public partial class CandidatesAllocationDialog
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Inject] private IBlazCandidateService BlazCandidateService { get; set; } = default!;

        [Inject] private IBlazVenueService BlazVenueService { get; set; } = default!;

        [Inject] private IDialogService DialogService { get; set; } = default!;

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public long SchedulePaperId { get; set; }

        [Parameter] public List<long> ScheduleVenuesIds { get; set; }

        private List<GetVenueResponseDto> AllAvailableVenues = [];

        private List<GetVenueResponseDto> ScheduleVenues = [];

        private GenericOrganizationStructureControlPanel GenericOrganizationStructureControlPanelRefAllocate;

        private bool isProcessingFile = false;

        private GetVenueResponseDto _selectedVenue;

        private List<string> ErrorListDto = [];

        protected override async Task OnInitializedAsync()
        {
            AllAvailableVenues = await BlazVenueService.GetAllVenuesAsync();

            ScheduleVenues = AllAvailableVenues.Where(x => ScheduleVenuesIds.Contains(x.Id)).ToList();
        }

        private async Task SubmitAllocation()
        {
            isProcessingFile = true;
            StateHasChanged();

            if (_selectedVenue is null)
            {
                Snackbar.Add(Resource.PleaseSelectVenueFirst, Severity.Warning);
                isProcessingFile = false;
                return;
            }

            var LookUpsObj = GenericOrganizationStructureControlPanelRefAllocate.GetSelectedLookupItems();

            if (LookUpsObj.Length == 0)
            {
                Snackbar.Add(Resource.PleaseSelectAtLeastOneOrganizationNode, Severity.Warning);
                isProcessingFile = false;
                StateHasChanged();
                return;
            }

            var response = await BlazCandidateService.AllocateCandidateByLookUpsIds(new AllocateLookUpsSchedulePaperRequestDto([.. LookUpsObj.Select(x => x.Id)], _selectedVenue.Id, SchedulePaperId));

            isProcessingFile = false;
            StateHasChanged();

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(response.Message, Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private async Task ShowAllocationsDialog()
        {
            var parameters = new DialogParameters
            {
                { nameof(AllocationsDialog.SchedulePaperId), SchedulePaperId }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            await DialogService.ShowAsync<AllocationsDialog>(string.Empty, parameters, options);
        }

        private void Cancel() => MudDialog.Close(DialogResult.Ok(true));
    }
}
