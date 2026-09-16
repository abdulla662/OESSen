using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Schedule;
using OES.Blazor.Pages.Schedule.Main.ScheduleCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Services.Interfaces.Notification;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Schedule.Main.ScheduleCreationOrUpdateStepper.ThirdStep
{
    public partial class SchedulePapersList
    {
        [Inject] public IBlazSchedulePaperService BlazSchedulePaperService { get; set; }

        [Inject] private IDialogService DialogService { get; set; }

        [Inject] public ISnackbar Snackbar { get; set; }

        [Inject] private INotificationManager NotificationManager { get; set; }


        [Parameter] public ScheduleMetadataResultedParamsDto ScheduleMetadataResultedParamsDto { get; set; } = new();


        private CustomTableData<SchedulePaperPaginationDto> _customTableDataOfSchedulePapers = new([], 0);

        private int _schedulePapersListKey = 0;


        protected override async Task OnInitializedAsync()
        {
            await NotificationManager.InitializeAsync();

            NotificationManager.OnNotificationReceived += NotificationReceived;
        }

        public async Task<CustomTableData<SchedulePaperPaginationDto>> GetAllSchedulePapersAsync(PaginationSearchModel paginationSearchModel)
        {
            _customTableDataOfSchedulePapers = await BlazSchedulePaperService.GetAllSchedulePapersByScheduleIdAsync(paginationSearchModel, ScheduleMetadataResultedParamsDto.ScheduleMetadataId);

            return _customTableDataOfSchedulePapers;
        }

        private void NotificationReceived(NotificationDto notification)
        {
            _schedulePapersListKey++;
            StateHasChanged();
        }

        private async Task AddSchedulePaperAsync()
        {
            var parameters = new DialogParameters<AddOrUpdateSchedulePaperDialog>
            {
                { x => x.ScheduleMetadataResultedParamsDto, ScheduleMetadataResultedParamsDto},
                { x => x.CurrentSchedulePapersList, _customTableDataOfSchedulePapers.Items }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                BackdropClick = false
            };

            var dialog = await DialogService.ShowAsync<AddOrUpdateSchedulePaperDialog>(Resource.AddSchedulePaper, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _schedulePapersListKey--;
            }
        }

        private async Task ConfigureSchedulePaperSettingsAsync(SchedulePaperPaginationDto selectedSchedulePaper)
        {
            var parameters = new DialogParameters<ConfigureSchedulePaperSettingsDialog>
            {
                { x => x.PaperId, selectedSchedulePaper.PaperId },
                { x => x.SchedulePaperId, selectedSchedulePaper.Id },
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<ConfigureSchedulePaperSettingsDialog>(Resource.ConfigureSchedulePaperSettingsTitle, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _schedulePapersListKey--;
            }
        }

        private async Task UpdateSchedulePaperAsync(object schedulePaperId)
        {
            var parameters = new DialogParameters<AddOrUpdateSchedulePaperDialog>
            {
                { x => x.ScheduleMetadataResultedParamsDto, ScheduleMetadataResultedParamsDto},
                { x => x.CurrentSchedulePapersList, _customTableDataOfSchedulePapers.Items },
                { x => x.SchedulePaperId, (long)schedulePaperId }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                BackdropClick = false
            };

            var dialog = await DialogService.ShowAsync<AddOrUpdateSchedulePaperDialog>(Resource.UpdateSchedulePaperTitle, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _schedulePapersListKey--;
            }
        }

        private async Task DeleteSchedulePaperAsync(object schedulePaperId)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.Alert },
                { x => x.Content, Resource.DeleteSchedulePaperConfirmation },
                { x => x.SubmitText, Resource.Delete },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var opts = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<GenericDialog>(Resource.DeleteSchedulePaperTitle, parameters, opts);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazSchedulePaperService.DeleteSchedulePaperAsync((long)schedulePaperId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _schedulePapersListKey--;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }
            }

            StateHasChanged();
        }

        public bool ValidateSchedulePapersListStepToProceed()
        {
            if (!_customTableDataOfSchedulePapers.Items.Any())
            {
                Snackbar.Add(Resource.PleaseAddAtLeastOneSchedulePaperBeforeProceeding, Severity.Error);
                return false;
            }

            if (_customTableDataOfSchedulePapers.Items.Any(x => !x.PaperSettingsConfigured))
            {
                Snackbar.Add(Resource.PleaseConfigureAllSchedulePapersSettingsBeforeProceeding, Severity.Error);
                return false;
            }

            return true;
        }
    }
}