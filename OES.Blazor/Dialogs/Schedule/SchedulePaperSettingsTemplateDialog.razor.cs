using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.PaperSetting.Requests;

namespace OES.Blazor.Dialogs.Schedule;

public partial class SchedulePaperSettingsTemplateDialog : ComponentBase
{
    [Inject] IBlazPaperSettingsService BlazPaperSettingService { get; set; }
    [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

    public PaperSettingsTemplateRequestDto PaperSettingsData { get; set; } = new();
    private bool IsReadOnly { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        var id = await BlazSessionStorageService.GetValue<long>("PerformViewBtnClick");

        var response = await BlazPaperSettingService.GetPaperSettingsTemplateByIdAsync(id);

        PaperSettingsData = (PaperSettingsTemplateRequestDto)response.Data;

        StateHasChanged();
    }
}