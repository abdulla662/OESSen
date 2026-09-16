using Microsoft.AspNetCore.Components;

namespace OES.Blazor.Components.TakeExam.ExamPanel.LeftPanel.Tools
{
    public partial class Notepad
    {
        [Parameter] public EventCallback CloseNotePad { get; set; }
    }
}
