using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Dialogs.CandidateBatchImportHistory
{
    public partial class ViewDistributionDialog : ComponentBase
    {
        [Inject] private IBlazSchedulePaperService BlazSchedulePaperService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;

        [CascadingParameter] public MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public long PaperId { get; set; }
        [Parameter] public string PaperName { get; set; } = string.Empty;
        [Parameter] public long TotalCandidates { get; set; }

        public async Task<CustomTableData<VenueCandidatesCountDto>> GetVenuesWithCandidatesCountAsync(PaginationSearchModel PaginationSearchModel)
        {
            return await BlazSchedulePaperService.GetVenuesWithCandidatesCountByPaperIdAsync(PaginationSearchModel, PaperId);
        }

        public void Close() => MudDialog?.Close();

        private async Task OpenDetailsDialogAsync(VenueCandidatesCountDto venue)
        {
            var dialogParameters = new DialogParameters<CandidateBatchDetailsDialog>
            {
                { x => x.VenueId, venue.VenueId },
                { x => x.VenueName, venue.VenueName },
                { x => x.PaperId, PaperId }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            await DialogService.ShowAsync<CandidateBatchDetailsDialog>(Resource.BatchDetails, dialogParameters, options);
        }
    }
}
