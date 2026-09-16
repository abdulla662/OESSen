using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning;
using OES.Helper.ResourceFiles;
using QuestionTypeEnum = OES.Helper.Enums.QuestionType;


namespace OES.Blazor.Dialogs.Paper.Sectioning.AutoSectioning
{
    public partial class DistributionSummaryDialog : ComponentBase
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public Dictionary<string, List<SummaryItem>> SummaryData { get; set; } = default!;

        [Parameter] public long TotalDistributed { get; set; }

        [Parameter] public int TotalRequired { get; set; }


        private bool _expandAll = true;
        private readonly Dictionary<string, bool> _sectionExpanded = [];


        protected override void OnInitialized()
        {
            if (SummaryData != null)
            {
                foreach (var section in SummaryData.Keys)
                {
                    _sectionExpanded[section] = true;
                }
            }
        }

        private void ToggleExpandAll()
        {
            _expandAll = !_expandAll;

            foreach (var section in SummaryData.Keys)
            {
                _sectionExpanded[section] = _expandAll;
            }
        }

        private static string GetQuestionTypeIcon(string questionTypeName) => questionTypeName switch
        {
            nameof(QuestionTypeEnum.MCQ) => Icons.Material.Filled.RadioButtonChecked,
            nameof(QuestionTypeEnum.Essay) => Icons.Material.Filled.Edit,
            nameof(QuestionTypeEnum.Comprehension) => Icons.Material.Filled.MenuBook,
            nameof(QuestionTypeEnum.MultipleCorrectAnswers) => Icons.Material.Filled.CheckBox,
            nameof(QuestionTypeEnum.TrueAndFalse) => Icons.Material.Filled.ToggleOn,
            _ => Icons.Material.Filled.Help
        };

        private static string GetDifficultyColor(string difficulty) => difficulty.ToLower() switch
        {
            "hard" => "#ff5252",
            "high" => "#ff5252",
            "medium" => "#ff9800",
            "easy" => "#4caf50",
            "low" => "#4caf50",
            _ => "#0288d1"
        };

        private Severity GetCompletionColor()
        {
            return TotalDistributed == TotalRequired ? Severity.Success : Severity.Warning;
        }

        private string GetCompletionText()
        {
            if (TotalDistributed == TotalRequired)
                return Resource.AllQuestionsHaveBeenSuccessfullyDistributed;
            else if (TotalDistributed < TotalRequired)
                return $"{Resource.YouNeedToDistribute} {TotalRequired - TotalDistributed} {Resource.MoreQuestions}.";
            else
                return $"{Resource.YouHaveDistributed} {TotalDistributed - TotalRequired} {Resource.TooManyQuestions}.";
        }

        private void CloseDialog() => MudDialog.Close();
    }
}
