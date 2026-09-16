using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Dialogs.Question.QuestionTemplate
{
    public partial class QuestionTemplate
    {
        [Inject] IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }
        [Parameter] public EventCallback<long> OnQuestionSelected { get; set; }
        [Parameter] public bool IsFromQuestionAI { get; set; }

        private int listChangingKey;


        private void GetQuestionById(long questionId)
        {
            OnQuestionSelected.InvokeAsync(questionId);
            MudDialog.Close(DialogResult.Ok(questionId));
        }

        private async Task DeleteQuestionTemplateAsync(object questionTemplateId)
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.ConfirmDelete },
                { p => p.Content, Resource.AreYouSureYouWantToDeleteThisTemplate },
                { p => p.SubmitText, Resource.Delete },
                { p => p.CancelText, Resource.Cancel },
                { p => p.SubmitButtonColor, Color.Error },
                { p => p.SubmitButtonStartIcon, Icons.Material.Filled.Delete },
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _ = long.TryParse(questionTemplateId.ToString(), out long parsedQuestionTemplateId);

                var response = await BlazQuestionService.DeleteQuestionTemplateAsync(parsedQuestionTemplateId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    listChangingKey++;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }
            }

            StateHasChanged();
        }

        private void Close() => MudDialog.Cancel();
    }
}
