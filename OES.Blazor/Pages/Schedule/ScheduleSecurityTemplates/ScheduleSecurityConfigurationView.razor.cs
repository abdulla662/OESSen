using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ScheduleSecurityConfigurationsService;
using OES.Helper.Dtos.ScheduleSecurityConfiguration;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Schedule.ScheduleSecurityTemplates
{
    public partial class ScheduleSecurityConfigurationView
    {
        [Inject] private IBlazSecurityConfigurationsService SecurityService { get; set; } = default!;

        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; } = default!;

        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private ExamSecurityConfigurationDto Model { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            var id = await BlazSessionStorageService.GetValue<long>("PerformViewBtnClick");
            var response = await SecurityService.GetSecurityConfigurationsByScheduleIdAsync(id);

            if (response != null)
            {
                Model = response;
            }
            else
            {
                Snackbar.Add(Resource.NoTemplatesAvailable, Severity.Error);
                NavigationManager.NavigateTo("/ScheduleSecurityTemplates");
            }
        }
    }
}

