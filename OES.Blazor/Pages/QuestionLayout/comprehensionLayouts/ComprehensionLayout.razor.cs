using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.HelperDtos;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Enums;
using System.Net;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.QuestionLayout.comprehensionLayouts
{
    public partial class ComprehensionLayout : ComponentBase
    {
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; } // Used to get the target question metadata id and target language id ..
        [Parameter] public bool Backward { get; set; } = true;
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;

        private int ColumnsCount { get; set; }
        private ComprehensionQuestionWithSubQuestionsDto ComprehensionQuestion { get; set; } = null;

        private static readonly Regex HighlightRegex = new(
            @"<span[^>]*class=""?marked-answer""?[^>]*>(.*?)<\/span>|\{\{([^_]+)_\}\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            TimeSpan.FromSeconds(1)
        );

        private int _currentQuestionIndex = 0;


        protected override void OnParametersSet()
        {
            if (Orientation == LayoutOrientation.Horizontal || Orientation == LayoutOrientation.HorizontalNext)
                ColumnsCount = 6;
            else if (Orientation == LayoutOrientation.Vertical || Orientation == LayoutOrientation.VerticalNext)
                ColumnsCount = 12;
        }

        protected override async Task OnInitializedAsync()
        {
            var response = await BlazQuestionService.GetComprehensionSubQuestionsAsync(Model.QuestionMetadataId, Model.LanguageId);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Error);
                return;
            }

            var subQuestionsList = (List<SubQuestionDetailsDto>)response.Data;

            ComprehensionQuestion = new ComprehensionQuestionWithSubQuestionsDto
            {
                ComprehensionBody = new SubQuestionDetailsDto
                {
                    Id = Model.Id,
                    Body = Model.Body,
                    Choices = Model.Choices,
                    Instructions = Model.Instructions,
                    ModelAnswer = Model.ModelAnswer,
                    LanguageId = Model.LanguageId,
                    QuestionMetadataId = Model.QuestionMetadataId,
                    AttachmentFileName = Model.AttachmentFileName
                },
                SubQuestionsList = subQuestionsList
            };

            StateHasChanged();
        }

        private async Task NextQuestion()
        {
            if (_currentQuestionIndex < ComprehensionQuestion.SubQuestionsList.Count - 1)
            {
                _currentQuestionIndex++;
                StateHasChanged();
            }

            await Task.CompletedTask;
        }

        private async Task PreviousQuestion()
        {
            if (_currentQuestionIndex > 0)
            {
                _currentQuestionIndex--;
                StateHasChanged();
            }

            await Task.CompletedTask;
        }

        private string? GetCorrectChoiceText(SubQuestionDetailsDto subQuestion)
        {
            if (!ShowCorrectAnswer || subQuestion?.Choices == null)
                return null;

            return subQuestion.Choices.FirstOrDefault(c => c.IsCorrectAnswer)?.ChoiceText;
        }

        private static string ConvertPlaceholdersToHighlight(string content, bool showCorrectAnswer)
        {
            if (string.IsNullOrEmpty(content))
                return "";

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

        private QuestionDataDto MapToQuestionData(SubQuestionDetailsDto subQuestion)
        {
            return new QuestionDataDto
            {
                Id = subQuestion.Id,
                Body = subQuestion.Body,
                Instructions = subQuestion.Instructions,
                Choices = subQuestion.Choices,
                ModelAnswer = subQuestion.ModelAnswer,
                QuestionMetadataId = subQuestion.QuestionMetadataId,
                LanguageId = Model.LanguageId,
                HasAttachment = subQuestion.HasAttachment,
                AttachmentFileName = subQuestion.AttachmentFileName
            };
        }
    }
}
