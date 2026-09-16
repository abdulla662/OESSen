using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Enums;

namespace OES.Blazor.Pages.QuestionLayout.OrderingLayout
{
    public partial class OrderingQuestionLayout
    {
        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;
    }
}

