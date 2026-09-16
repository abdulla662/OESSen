using Microsoft.AspNetCore.Components;
using OES.Blazor.Pages.Schedule.PaperSettingTemplates.Common;
using OES.Helper.Dtos.PaperSetting.Requests;

namespace OES.Blazor.Pages.Schedule.PaperSettingTemplates
{
    public partial class CreatePaperSettingTemplate : ComponentBase
    {
        public PaperSettingsTemplateRequestDto PaperSettingsData { get; set; } = new();

        public bool IsSaveTemplateDisabled => string.IsNullOrWhiteSpace(PaperSettingsData?.Name);

        private PaperSettingComponent paperSettingTemplate;

        private readonly bool IsReadOnly = false;
    }
}
