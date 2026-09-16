using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Paper.Responses;
using QuestionTypeEnum = OES.Helper.Enums.QuestionType;

namespace OES.Blazor.Dialogs.Paper.PaperSummary
{
    public partial class SummaryDialog
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = null!;

        [Parameter] public SectionItemBankSummaryResponseDto ItemBank { get; set; } = null!;

        private void Close() => MudDialog.Cancel();

        private static string GetQuestionTypeIcon(string questionTypeName) => questionTypeName switch
        {
            nameof(QuestionTypeEnum.MCQ) => Icons.Material.Filled.RadioButtonChecked,
            nameof(QuestionTypeEnum.Essay) => Icons.Material.Filled.Edit,
            nameof(QuestionTypeEnum.Comprehension) => Icons.Material.Filled.MenuBook,
            nameof(QuestionTypeEnum.MultipleCorrectAnswers) => Icons.Material.Filled.CheckBox,
            nameof(QuestionTypeEnum.TrueAndFalse) => Icons.Material.Filled.ToggleOn,
            _ => Icons.Material.Filled.Help
        };
    }
}
