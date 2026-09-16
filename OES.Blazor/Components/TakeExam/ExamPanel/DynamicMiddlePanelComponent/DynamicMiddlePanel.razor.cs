using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Dtos.Question;
using OES.Helper.Enums;

namespace OES.Blazor.Components.TakeExam.ExamPanel.DynamicMiddlePanelComponent
{
    public partial class DynamicMiddlePanel
    {
        [Parameter] public List<string> AllSections { get; set; } = [];
        [Parameter] public long QuestionMetaDataId { get; set; }
        [Parameter] public QuestionWithScoreDto Question { get; set; }
        [Parameter] public GetQuestionMetaDataForQCViewDto GetQuestionMetaDataForQCViewDto { get; set; }

        private QuestionWithScoreDto Model { get; set; } = new();
        private LayoutOrientation Orientation { get; set; } = LayoutOrientation.Horizontal;
        private Breakpoint SelectedScreenSize { get; set; } = Breakpoint.Md;
        private string QuestionType { get; set; } = string.Empty;
        private string SelectScreenSizeClass { get; set; } = "w-100";
        private bool IsInitialized { get; set; }
        private string SectionName { get; set; } = string.Empty;
        private bool IsTimer { get; set; } = false;
        private int ActiveSectionIndex { get; set; } = 0;


        protected override void OnInitialized()
        {
            SelectedScreenSize = Breakpoint.Md;
            SelectScreenSizeClass = "w-100";
            IsInitialized = true;

            if (Question?.Metadata != null)
            {
                QuestionType = Question.Metadata.Type ?? string.Empty;
                Orientation = LayoutOrientation.Horizontal;
                SectionName = Question.SectionName ?? string.Empty;
                IsTimer = Question.IsTimer;
                ActiveSectionIndex = GetActiveSectionIndex();
                MapQuestionToModel();
            }

            StateHasChanged();
        }

        private void MapQuestionToModel()
        {
            if (Question?.Metadata == null) return;

            Model = Question;
        }

        private int GetActiveSectionIndex()
        {
            if (string.IsNullOrWhiteSpace(SectionName) || AllSections == null || AllSections.Count == 0)
                return 0;

            var index = AllSections.IndexOf(SectionName);

            return index >= 0 ? index : 0;
        }
    }
}
