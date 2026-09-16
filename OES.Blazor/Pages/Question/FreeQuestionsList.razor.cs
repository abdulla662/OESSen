using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Question.UploadFreeQuestionsDialog;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Question
{
    public partial class FreeQuestionsList : ComponentBase
    {
        [Inject] IDialogService DialogService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IBlazQuestionService BlazQuestionService { get; set; }

        private int _questionsListChangingKey;
        private HashSet<StandaloneQuestionsResponseDto> _selectedQuestions = new();

        protected Task OnSelectedQuestionsChanged(HashSet<StandaloneQuestionsResponseDto> selected)
        {
            _selectedQuestions = selected;
            StateHasChanged();
            return Task.CompletedTask;
        }

        private async Task ShowResetConfirmation()
        {
            if (_selectedQuestions == null || _selectedQuestions.Count == 0)
            {
                Snackbar.Add(Resource.PleaseSelectAtLeastOneQuestion, Severity.Warning);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Content, string.Format(Resource.ConfirmResetQuestions, _selectedQuestions.Count) },
                { x => x.SubmitText, Resource.Yes },
                { x => x.CancelText, Resource.No },
                { x => x.SubmitButtonColor, Color.Primary },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Check }
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                CloseButton = false
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await ResetSelectedQuestionsAsync();
            }
        }

        private async Task ResetSelectedQuestionsAsync()
        {
            if (_selectedQuestions.Count == 0)
                return;

            var selectedCodes = _selectedQuestions.Select(q => q.QuestionCode).ToList();

            var dto = new UpdateQuestionsCreationStatusRequestDto(
                selectedCodes,
                QuestionStatus.LayoutSelectedAndPending
            );

            var response = await BlazQuestionService.ChangeQuestionsCreationStatusAsync(dto);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(Resource.Updated, Severity.Success);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            _selectedQuestions.Clear();
            _questionsListChangingKey++;
        }

        private async Task OpenUploadDialog()
        {
            var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseOnEscapeKey = false };
            await DialogService.ShowAsync<UploadFreeQuestionsDialog>(string.Empty, options);
        }
    }
}