using Microsoft.AspNetCore.Components;
using OES.Blazor.Pages.Schedule.PaperSettingTemplates.Common;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.PaperSetting.Requests;

namespace OES.Blazor.Pages.Schedule.PaperSettingTemplates
{
    public partial class EditPaperSettingTemplate : ComponentBase
    {
        [Inject] IBlazPaperSettingsService _blazPaperSettingService { get; set; }
        [Inject] IBlazSessionStorageService _blazSessionStorageService { get; set; }

        public PaperSettingsTemplateRequestDto PaperSettingsData { get; set; } = new();

        public bool IsSaveTemplateDisabled => string.IsNullOrWhiteSpace(PaperSettingsData?.Name);

        private PaperSettingComponent paperSettingTemplate;

        private readonly bool IsReadOnly = false;

        protected override async Task OnInitializedAsync()
        {
            var id = await _blazSessionStorageService.GetValue<long>("PerformEditBtnClick");

            if (id != 0)
            {
                var response = await _blazPaperSettingService.GetPaperSettingsTemplateByIdAsync(id);

                PaperSettingsData = (PaperSettingsTemplateRequestDto)response.Data;
            }

            StateHasChanged();
        }
    }
}
