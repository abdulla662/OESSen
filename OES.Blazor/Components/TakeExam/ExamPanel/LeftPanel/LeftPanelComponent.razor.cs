using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Enums;
using OES.Helper.General;
using System.Globalization;

namespace OES.Blazor.Components.TakeExam.ExamPanel.LeftPanel;

public partial class LeftPanelComponent
{
    [Inject] IJSRuntime JS { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;

    [Parameter] public ExamViewStatus ViewStatus { get; set; }
    [Parameter] public List<QuestionWithScoreDto> Questions { get; set; } = new();
    [Parameter] public EventCallback<long> OnQuestionSelected { get; set; }
    [Parameter] public QuestionWithScoreDto CurrentQuestion { get; set; }

    private int CurrentIndex { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            bool isArabic = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

            await JS.InvokeVoidAsync(MiscConstants.ToggleArabicNumerals, isArabic);
        }
    }

    private async Task SelectQuestion(int index)
    {
        if (index >= Questions.Count)
            return;

        CurrentIndex = index;

        await OnQuestionSelected.InvokeAsync(index);
    }

    public async Task GoToNextQuestion()
    {
        if (Questions == null || Questions.Count == 0)
            return;

        int nextIndex = CurrentIndex + 1;

        if (nextIndex >= Questions.Count)
            return;

        CurrentIndex = nextIndex;

        await OnQuestionSelected.InvokeAsync(CurrentIndex);
    }

    public async Task GoToPreviousQuestion()
    {
        if (Questions == null || Questions.Count == 0)
            return;

        int prevIndex = CurrentIndex - 1;

        if (prevIndex < 0)
            return;

        CurrentIndex = prevIndex;

        await OnQuestionSelected.InvokeAsync(CurrentIndex);
    }

    private void EndExamPreview()
    {
        var finalPath = ViewStatus == ExamViewStatus.Dynamic ? "/UserPapers" : "/Questions";

        NavigationManager.NavigateTo(finalPath);
    }
}