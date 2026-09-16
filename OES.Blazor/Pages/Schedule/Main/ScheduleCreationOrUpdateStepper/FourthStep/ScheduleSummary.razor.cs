using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos.ScheduleSummary;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using System.Net;

namespace OES.Blazor.Pages.Schedule.Main.ScheduleCreationOrUpdateStepper.FourthStep
{
    public partial class ScheduleSummary : ComponentBase
    {
        [Inject] private IDialogService DialogService { get; set; }

        [Inject] private IBlazScheduleService BlazScheduleService { get; set; }

        [Inject] private IBlazSchedulePaperService BlazSchedulePaperService { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; }

        [Inject] private NavigationManager NavigationManager { get; set; }

        [Parameter] public long ScheduleId { get; set; }

        private bool IsSchedulePublishedSuccessfully => _scheduleSummary.SchedulePublishingStatus == PublishingStatus.Published || _isSchedulePublished;

        private GetScheduleSummaryDto _scheduleSummary;

        private bool _isLoading = true;

        private bool _isSchedulePublished;


        // DATA PROCESSING METHODS

        protected override async Task OnInitializedAsync()
        {
            _isLoading = true;

            StateHasChanged();

            _scheduleSummary = await BlazSchedulePaperService.GetSchedulePaperSummaryAsync(ScheduleId);

            if (_scheduleSummary != null)
            {
                _isLoading = false;

                StateHasChanged();
            }
        }

        private async Task PublishScheduleAsync()
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.Alert },
                { p => p.Content, Resource.PublishScheduleConfirm },
                { p => p.SubmitText, Resource.Yes },
                { p => p.CancelText, Resource.Cancel },
                { p => p.SubmitButtonStartIcon, Icons.Material.Filled.CheckCircle },
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazScheduleService.PublishScheduleAsync(ScheduleId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(response.Message, Severity.Success);
                    _isSchedulePublished = true;
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }
            }
        }


        // HELPER METHODS

        private static Color GetPaperTypeColor(PaperType paperType)
        {
            return paperType switch
            {
                PaperType.Standard => Color.Warning,
                PaperType.Adaptive => Color.Info,
                _ => Color.Default
            };
        }

        private void NavigateToHome()
        {
            NavigationManager.NavigateTo("/");
        }
    }
}