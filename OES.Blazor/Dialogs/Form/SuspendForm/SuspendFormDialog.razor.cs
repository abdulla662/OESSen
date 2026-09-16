using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces;
using OES.Helper.Dtos.QueueSuspend;
using OES.Helper.General;
using System.Net;

namespace OES.Blazor.Dialogs.Form.SuspendForm
{
    public partial class SuspendFormDialog
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public long FormId { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Inject] private IBlazQueueSuspendService QueueService { get; set; }

        private HashSet<string> _selectedVenueCodes = [];
        private bool _isProcessing = false;
        private List<QueueSuspendResponseDto> _currentPageVenues = [];
        private bool _isInitialLoading;

        protected override async Task OnInitializedAsync()
        {
            _isInitialLoading = true;
            try
            {
                var suspendedVenueCodes = await QueueService.GetSuspendedVenueCodesByFormIdAsync(FormId);
                foreach (var venueCode in suspendedVenueCodes)
                    _selectedVenueCodes.Add(venueCode);
            }
            finally
            {
                _isInitialLoading = false;
            }
        }

        private bool IsVenueSelected(QueueSuspendResponseDto venue)
            => _selectedVenueCodes.Contains(venue.VenueCode);

        private void ToggleVenueSelection(QueueSuspendResponseDto venue, bool isSelected)
        {
            if (isSelected)
                _selectedVenueCodes.Add(venue.VenueCode);
            else
                _selectedVenueCodes.Remove(venue.VenueCode);

            StateHasChanged();
        }

        private void ToggleSelectAllCurrentPage(bool isSelected)
        {
            foreach (var venue in _currentPageVenues)
            {
                if (isSelected)
                    _selectedVenueCodes.Add(venue.VenueCode);
                else
                    _selectedVenueCodes.Remove(venue.VenueCode);
            }

            StateHasChanged();
        }

        private async Task<CustomTableData<QueueSuspendResponseDto>> GetPaginatedVenuesAsync(PaginationSearchModel paginationSearchModel)
        {
            var result = await QueueService.GetPaginatedFormVenuesForSuspensionAsync(paginationSearchModel, FormId);

            _currentPageVenues = result?.Items?.ToList() ?? [];

            return result ?? new CustomTableData<QueueSuspendResponseDto>([], 0);
        }

        private void CloseDialog() => MudDialog.Close();

        private async Task ConfirmSuspend()
        {
            _isProcessing = true;
            StateHasChanged();

            try
            {
                var request = new ManageFormSuspensionsDto
                {
                    FormId = FormId,
                    SuspendedVenueCodes = _selectedVenueCodes.ToList()
                };

                var result = await QueueService.ManageFormSuspensionsAsync(request);

                if (result.StatusCode == HttpStatusCode.OK || result.StatusCode == HttpStatusCode.MultiStatus)
                {
                    var severity = result.StatusCode == HttpStatusCode.OK ? Severity.Success : Severity.Warning;

                    if (severity == Severity.Warning)
                    {
                        Snackbar.Add(result.Message, severity, config =>
                        {
                            config.HideTransitionDuration = 100;
                            config.ShowTransitionDuration = 100;
                            config.VisibleStateDuration = 10000;
                        });
                    }
                    else
                    {
                        Snackbar.Add(result.Message, severity);
                    }

                    MudDialog.Close(DialogResult.Ok(result.Data));
                }
                else
                {
                    Snackbar.Add(result.Message, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add(ex.Message, Severity.Error);
            }
            finally
            {
                _isProcessing = false;
                StateHasChanged();
            }
        }
    }
}
