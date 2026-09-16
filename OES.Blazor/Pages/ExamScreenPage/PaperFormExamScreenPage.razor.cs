using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.Form;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.General;

namespace OES.Blazor.Pages.ExamScreenPage
{
    public partial class PaperFormExamScreenPage
    {
        [Inject] private IBlazFormService BlazFormService { get; set; } = default!;

        [Inject] private IBlazSessionStorageService SessionStorage { get; set; } = default!;

        private List<QuestionWithScoreDto> _questions = [];

        private bool _loading = true;

        protected override async Task OnInitializedAsync()
        {
            var paperId = await SessionStorage.GetValue<long>(MiscConstants.PerformEditBtnClick);

            var formId = await SessionStorage.GetValue<long>(MiscConstants.ExamScreenFormId);

            var paper = await BlazFormService.GetAllFormsWithTheirQuestionsByPaperIdAsync(paperId);

            var form = paper?.Forms?.FirstOrDefault(f => f.FormId == formId);

            if (form?.Questions != null)
            {
                _questions = [.. form.Questions.OrderBy(q => q.OrderId)];
            }

            _loading = false;
        }
    }
}
