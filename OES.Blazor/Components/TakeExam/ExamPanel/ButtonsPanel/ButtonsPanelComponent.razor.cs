using Microsoft.AspNetCore.Components;

namespace OES.Blazor.Components.TakeExam.ExamPanel.ButtonsPanel
{
    public partial class ButtonsPanelComponent
    {
        [Parameter] public EventCallback OnPreviousClicked { get; set; }

        [Parameter] public EventCallback OnNextClicked { get; set; }
    }
}
