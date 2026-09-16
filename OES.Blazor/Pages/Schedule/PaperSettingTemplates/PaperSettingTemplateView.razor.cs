using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.PaperSetting.Responses;

namespace OES.Blazor.Pages.Schedule.PaperSettingTemplates
{
    public partial class PaperSettingTemplateView : ComponentBase
    {
        [Inject] private IBlazPaperSettingsService BlazPaperSettingService { get; set; }

        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; } = default!;

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

        private bool _isLoading = true;

        private GetPaperSettingsResponseDto _paperSettingsResponseDto = null;

        protected override async Task OnInitializedAsync()
        {
            var schedulePaperId = await BlazSessionStorageService.GetValue<long>("SchedulePaperId");

            await LoadPaperSettingsAsync(schedulePaperId);
        }

        private async Task LoadPaperSettingsAsync(long schedulePaperId)
        {
            _paperSettingsResponseDto = null;

            _isLoading = true;
            StateHasChanged();

            _paperSettingsResponseDto = await BlazPaperSettingService.GetPaperSettingsBySchedulePaperIdAsync(schedulePaperId);

            _isLoading = false;
            StateHasChanged();
        }
    }
}