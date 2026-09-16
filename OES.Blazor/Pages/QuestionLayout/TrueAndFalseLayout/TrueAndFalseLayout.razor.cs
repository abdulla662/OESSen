using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using System.Globalization;

namespace OES.Blazor.Pages.QuestionLayout.TrueAndFalseLayout
{
    public partial class TrueAndFalseLayout : ComponentBase
    {
        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;

        private string CorrectAnswerText { get; set; }

        private const string trueText = "true";
        private const string falseText = "false";

        protected override void OnParametersSet()
        {
            CorrectAnswerText = Model?.Choices?.Find(x => x.IsCorrectAnswer)?.ChoiceText.Trim().ToLower() ?? string.Empty;
        }

        private static string GetDirection()
        {
            var culture = CultureInfo.CurrentUICulture;
            return culture.TextInfo.IsRightToLeft ? "rtl" : "ltr";
        }

        private static string GetQuestionBodyClass()
        {
            return $"mcq-question-body mcq-content-{GetDirection()}";
        }
    }
}
