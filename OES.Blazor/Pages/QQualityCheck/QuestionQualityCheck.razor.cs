using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.QQualityCheck
{
    public partial class QuestionQualityCheck : ComponentBase
    {
        [Inject] IBlazSessionStorageService _blazSessionStorageService { get; set; }

        [Inject] IBlazQuestionService BlazQuestionService { get; set; } = default!;

        [Inject] ISnackbar Snackbar { get; set; } = default!;

        public long QuestionMetaDataId { get; set; }

        private int _renderKey = 0;

        private async Task OnQuestionChanged(long newId)
        {
            QuestionMetaDataId = newId;

            StateHasChanged();

            await Task.CompletedTask;
        }

        protected override async Task OnInitializedAsync()
        {
            QuestionMetaDataId = await _blazSessionStorageService.GetValue<long>("PerformEditBtnClick");

            StateHasChanged();
        }

        private async Task GoToNextQuestionWithSameStatus()
        {
            var nextId = await BlazQuestionService.GetNextQuestionIdAsync(QuestionMetaDataId);

            if (nextId.HasValue && nextId > 0)
            {
                QuestionMetaDataId = nextId.Value;

                _renderKey++;

                StateHasChanged();
            }
            else
            {
                Snackbar.Add(Resource.NoNextQuestion, Severity.Info);
            }
        }

        private async Task GoToPreviousQuestionWithSameStatus()
        {
            var prevId = await BlazQuestionService.GetPreviousQuestionIdAsync(QuestionMetaDataId);

            if (prevId.HasValue && prevId > 0)
            {
                QuestionMetaDataId = prevId.Value;

                _renderKey++;

                StateHasChanged();
            }
            else
            {
                Snackbar.Add(Resource.NoPreviousQuestion, Severity.Info);
            }
        }
    }
}
