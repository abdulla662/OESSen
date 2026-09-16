using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Dialogs.Question.InstructionTemplateDialog
{
    public partial class InstructionTemplateDialog : ComponentBase
    {
        [Inject] IBlazQuestionInstructionTemplate BlazInstructionTemplateService { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }
        [Parameter] public EventCallback<long> OnTemplateSelected { get; set; }

        private int listChangingKey;

        private void SelectInstructionTemplate(QuestionInstructionTemplateDto template)
        {
            MudDialog.Close(DialogResult.Ok(template));
        }

        private async Task DeleteInstructionTemplateAsync(object templateId)
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisTemplate },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _ = long.TryParse(templateId.ToString(), out long parsedTemplateId);

                var response = await BlazInstructionTemplateService.DeleteInstructionTemplateAsync(parsedTemplateId);

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
    }
}
