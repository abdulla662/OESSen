using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.MediaSettingService;
using OES.Helper.Dtos.MediaSetting.Requests;
using System.Net;

namespace OES.Blazor.Pages.MediaConfiguration
{
    public partial class AddMediaSetting : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazMediaSettingService BlazMediaSettingService { get; set; }

        private MediaSettingRequestDto Model { get; set; } = new();

        private const string MediaConfigurationPath = "/MediaConfiguration";
        private static int MaxAllowedSizeInKB => 30000;
        private bool IsSubmitButtonDisabled => Model.MaxSizeInKB < 1 || Model.MaxSizeInKB > MaxAllowedSizeInKB;

        private async Task OnSubmitAsync()
        {
            var response = await BlazMediaSettingService.AddMediaAsync(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigationManager.NavigateTo(MediaConfigurationPath);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Warning);
            }
        }

        private void Cancel() => NavigationManager.NavigateTo(MediaConfigurationPath);
    }
}