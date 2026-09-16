using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Enums;

namespace OES.Blazor.Pages.QuestionLayout.PaperAnsweredQuestion
{
    public partial class PaperAnsweredQuestionLayout : ComponentBase
    {
        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;

        private int ColumnsCount { get; set; }

        protected override void OnParametersSet()
        {
            if (Orientation == LayoutOrientation.Horizontal)
                ColumnsCount = 6;
            else if (Orientation == LayoutOrientation.Vertical)
                ColumnsCount = 12;
        }
    }
}