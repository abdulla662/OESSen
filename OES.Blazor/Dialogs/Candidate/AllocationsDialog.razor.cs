using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Enums;

namespace OES.Blazor.Dialogs.Candidate
{
    public partial class AllocationsDialog
    {
        [Inject] IBlazSchedulePaperService BlazeSchedulePaperService { get; set; } = default!;

        [Inject] ISnackbar Snackbar { get; set; } = default!;

        [Inject] IDialogService DialogService { get; set; } = default!;

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public long SchedulePaperId { get; set; }

        private bool isProcessingFile = false;

        private List<GetSchedulePaperAllocationResponseDto> getSchedulePaperAllocationResponseDto = [];

        protected override async Task OnInitializedAsync()
        {
            await LoadAllocations();
        }

        private async Task LoadAllocations()
        {
            isProcessingFile = true;
            StateHasChanged();

            var response = await BlazeSchedulePaperService.GetSchedulePaperAllocation(SchedulePaperId);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                getSchedulePaperAllocationResponseDto = (response.Data as List<GetSchedulePaperAllocationResponseDto>) ?? [];
            }
            else
            {
                getSchedulePaperAllocationResponseDto = [];
            }

            isProcessingFile = false;
            StateHasChanged();
        }

        private async Task RemoveAllocation(long venueId, long schedulePaperId)
        {
            if (isProcessingFile) return;

            var result = await DialogService.ShowMessageBox(
                "Confirm",
                "Are you sure you want to remove this allocation?",
                yesText: "Yes",
                cancelText: "No"
            );

            if (result != true) return;

            isProcessingFile = true;
            StateHasChanged();

            var response = await BlazeSchedulePaperService.DeletePaperAllocationAsync(venueId, schedulePaperId);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(response.Message, Severity.Success);
                await LoadAllocations();
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            isProcessingFile = false;
            StateHasChanged();
        }

        private void Cancel() => MudDialog.Close();
    }
}
