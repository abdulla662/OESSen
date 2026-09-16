using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.MediaSettingService;
using OES.Helper.Dtos.MediaSetting.Requests;
using OES.Helper.Dtos.MediaSetting.Responses;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.MediaConfiguration
{
    public partial class UpdateMediaSetting : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazMediaSettingService BlazMediaSettingService { get; set; }
        [Inject] IBlazSessionStorageService SessionStorageService { get; set; }

        private MediaSettingRequestDto Model { get; set; } = new();

        private const string MediaConfigurationPath = "/MediaConfiguration";
        private static int MaxAllowedSizeInKB => 30000;
        private bool IsSubmitButtonDisabled => Model.MaxSizeInKB < 1 || Model.MaxSizeInKB > MaxAllowedSizeInKB;

        protected override async Task OnInitializedAsync()
        {
            var idString = await SessionStorageService.GetValue<string>("PerformEditBtnClick");

            if (long.TryParse(idString, out long id))
            {
                var response = await BlazMediaSettingService.GetByIdAsync(id);

                if (response.StatusCode == HttpStatusCode.OK && response.Data is MediaSettingResponseDto setting)
                {
                    Model.MediaCategory = setting.MediaCategory;
                    Model.MaxSizeInKB = setting.MaxSizeInKB;
                }
                else
                {
                    Snackbar.Add(Resource.MediaNotFound, Severity.Error);
                    NavigationManager.NavigateTo(MediaConfigurationPath);
                }
            }
            else
            {
                NavigationManager.NavigateTo(MediaConfigurationPath);
            }
        }

        private async Task OnSubmitAsync()
        {
            var response = await BlazMediaSettingService.UpdateMediaAsync(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigationManager.NavigateTo(MediaConfigurationPath);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void Cancel() => NavigationManager.NavigateTo(MediaConfigurationPath);
    }
}
