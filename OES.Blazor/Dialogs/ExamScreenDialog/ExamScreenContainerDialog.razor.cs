using Microsoft.AspNetCore.Components;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Enums;

namespace OES.Blazor.Dialogs.ExamScreenDialog
{
    public partial class ExamScreenContainerDialog
    {
        [Parameter] public long SelectedQuestionId { get; set; } = 0;

        [Parameter] public List<QuestionWithScoreDto> Questions { get; set; } = [];

        [Parameter] public ExamViewStatus ExamViewStatus { get; set; } = ExamViewStatus.Static;
    }
}
