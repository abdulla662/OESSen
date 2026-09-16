using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Enums;

namespace OES.Blazor.Dialogs.Question.QuestionLayoutShowingDialog
{
    public partial class QuestionLayoutShowingDialog
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }
        [Parameter] public string QuestionType { get; set; }
        [Parameter] public Breakpoint SelectedScreenSize { get; set; }
        [Parameter] public string SelectScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; } = new();

        private void Cancel() => MudDialog.Cancel();
    }
}
