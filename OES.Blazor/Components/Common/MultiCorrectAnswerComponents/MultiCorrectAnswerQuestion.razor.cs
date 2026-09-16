using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.Enums;

namespace OES.Blazor.Components.Common.MultiCorrectAnswerComponents
{
    public partial class MultiCorrectAnswerQuestion
    {
        [Parameter] public MarkupString QuestionHeader { get; set; }
        [Parameter] public MarkupString QuestionInstructions { get; set; }
        [Parameter] public List<ChoiceDataDto> Options { get; set; } = [];
        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;
        [Parameter] public bool HasAttachment { get; set; }
        [Parameter] public string AttachmentFileUrl { get; set; }

        private int ColumnsCount { get; set; }

        private string OptionWidthStyle => $"width: {(Orientation == LayoutOrientation.Horizontal ? 6 : ColumnsCount) * 100 / 12}%;";

        protected override void OnParametersSet()
        {
            ColumnsCount = Orientation switch
            {
                LayoutOrientation.MinVertical => 3,
                _ => 12
            };
        }
    }
}