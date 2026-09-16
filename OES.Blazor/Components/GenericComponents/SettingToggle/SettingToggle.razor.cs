using Microsoft.AspNetCore.Components;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Components.GenericComponents.SettingToggle
{
    public partial class SettingToggle
    {
        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public string OperationsText { get; set; } = Resource.Operations;

        private bool isExpanded = false;

        private void ToggleActions()
        {
            isExpanded = !isExpanded;
        }
    }
}
