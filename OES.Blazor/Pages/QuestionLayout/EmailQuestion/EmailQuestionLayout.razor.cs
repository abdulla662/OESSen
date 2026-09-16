using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.EmailQuestionAnswerDto;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Enums;
using System.Text.Json;

namespace OES.Blazor.Pages.QuestionLayout.EmailQuestion
{
    public partial class EmailQuestionLayout : ComponentBase
    {
        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;

        private EmailQuestionAnswerDto? ParsedAnswer { get; set; }
        private int ColumnsCount { get; set; }

        protected override void OnParametersSet()
        {
            ColumnsCount = Orientation == LayoutOrientation.Horizontal ? 6 : 12;

            if (!string.IsNullOrWhiteSpace(Model?.ModelAnswer))
            {
                try
                {
                    ParsedAnswer = JsonSerializer.Deserialize<EmailQuestionAnswerDto>(
                        Model.ModelAnswer,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
                catch
                {
                    ParsedAnswer = null;
                }
            }
        }
    }
}
