using Microsoft.AspNetCore.Components;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Enums;

namespace OES.Blazor.Components.TakeExam.ExamPanel.ScreenHeader
{
    public partial class ScreenHeaderComponent
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        [Parameter] public List<QuestionWithScoreDto> Questions { get; set; } = [];
        [Parameter] public string ExamFormName { get; set; } = "Sample Exam Form";
        [Parameter] public ExamViewStatus ViewStatus { get; set; }

        private void EndExamPreview()
        {
            var finalPath = ViewStatus == ExamViewStatus.Dynamic ? "/UserPapers" : "/Questions";
            NavigationManager.NavigateTo(finalPath);
        }
    }
}