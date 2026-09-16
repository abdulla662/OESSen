using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OES.Blazor.Components.TakeExam.ExamPanel.LeftPanel;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Blazor.Components.TakeExam.ExamPanel
{
    public partial class ExamScreenContainer : IAsyncDisposable
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;

        [Parameter] public ExamViewStatus ViewStatus { get; set; } = ExamViewStatus.Static;

        [Parameter] public List<QuestionWithScoreDto> Questions { get; set; } = [];

        [Parameter] public long SelectedQuestionId { get; set; }

        private LeftPanelComponent LeftPanelRef { get; set; }

        private int CurrentIndex { get; set; } = 0;

        private List<string> AllSectionsCache { get; set; } = [];

        private QuestionWithScoreDto CurrentQuestion => Questions?.Count > 0 && CurrentIndex >= 0 && CurrentIndex < Questions.Count
            ? Questions[CurrentIndex]
            : null;

        protected override void OnInitialized()
        {
            AllSectionsCache = GetAllSections();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync(
                    MiscConstants.PageCssManagerRegister,
                    MiscConstants.ApplyExamStyleSheet
                );
            }
        }

        private void LoadQuestionInMiddlePanel(long index)
        {
            if (Questions == null || Questions.Count == 0)
            {
                return;
            }

            CurrentIndex = (int)index;

            StateHasChanged();
        }

        private async Task OnNextClicked()
        {
            if (LeftPanelRef != null)
            {
                await LeftPanelRef.GoToNextQuestion();

                StateHasChanged();
            }
        }

        private async Task OnPreviousClicked()
        {
            if (LeftPanelRef != null)
            {
                await LeftPanelRef.GoToPreviousQuestion();

                StateHasChanged();
            }
        }

        private List<string> GetAllSections()
        {
            return [.. Questions
                .Where(q => !string.IsNullOrEmpty(q.SectionName))
                .Select(q => q.SectionName)
                .Distinct()
            ];
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await JS.InvokeVoidAsync(MiscConstants.PageCssManagerUnregister);
            }
            catch
            {
                // Do nothing.
            }
        }
    }
}