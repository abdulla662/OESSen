using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.CBTSyncSettingService;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.CBTSyncSetting.Requests;
using OES.Helper.Dtos.CBTSyncSetting.Responses;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.CBTSyncSetting
{
    public partial class UpdateCBTSyncSetting : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazCBTSyncSettingService BlazCBTSyncSettingService { get; set; }
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

        private CBTSyncSettingRequestDto Model { get; set; } = new();
        private TimeSpan? _syncTime;

        protected override async Task OnInitializedAsync()
        {
            var id = await BlazSessionStorageService.GetValue<long>(MiscConstants.PerformEditBtnClick);

            var apiResponse = await BlazCBTSyncSettingService.GetByIdAsync(id);

            if (apiResponse.StatusCode == HttpStatusCode.OK && apiResponse.Data is CBTSyncSettingResponseDto data)
            {
                Model = new CBTSyncSettingRequestDto
                {
                    Id = data.Id,
                    CBTAutoSyncEnabled = data.CBTAutoSyncEnabled,
                    CBTSyncScheduleTime = data.CBTSyncScheduleTime
                };

                if (TimeSpan.TryParse(data.CBTSyncScheduleTime, out var time))
                {
                    _syncTime = time;
                }
            }
            else
            {
                Snackbar.Add(Resource.DataNotFound, Severity.Error);

                NavigationManager.NavigateTo("/CBTSyncSetting");
            }
        }

        private async Task OnSubmitAsync()
        {
            if (_syncTime.HasValue)
            {
                Model.CBTSyncScheduleTime = _syncTime.Value.ToString(@"hh\:mm");
            }

            var response = await BlazCBTSyncSettingService.UpdateAsync(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigationManager.NavigateTo("/CBTSyncSetting");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void Cancel() => NavigationManager.NavigateTo("/CBTSyncSetting");
    }
}
