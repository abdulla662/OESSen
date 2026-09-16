using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Enums;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.QuestionLayout.FillInTheBlank
{
    public partial class FillInTheBlank : ComponentBase
    {
        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;

        private int ColumnsCount { get; set; }

        private string FormattedBody => ConvertPlaceholdersToHighlight(Model?.Body ?? "", ShowCorrectAnswer);

        private static readonly Regex HighlightRegex = new(
            @"<span[^>]*class=""?marked-answer""?[^>]*>(.*?)<\/span>|\{\{([^_]+)_\}\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            TimeSpan.FromSeconds(1)
        );


        protected override void OnParametersSet()
        {
            if (Orientation == LayoutOrientation.Horizontal)
                ColumnsCount = 6;
            else if (Orientation == LayoutOrientation.Vertical)
                ColumnsCount = 12;
        }

        private static string ConvertPlaceholdersToHighlight(string content, bool showCorrectAnswer)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            return HighlightRegex.Replace(content, match =>
            {
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                    return match.Groups[1].Value;

                if (!string.IsNullOrEmpty(match.Groups[2].Value))
                {
                    if (!showCorrectAnswer)
                    {
                        return "_________";
                    }
                    else
                    {
                        return $"<span class='marked-answer' style='color: #4CAF50; padding: 2px 4px; border-radius: 3px; font-weight: bold;'>{match.Groups[2].Value}</span>";
                    }
                }

                return match.Value;
            });
        }
    }
}
