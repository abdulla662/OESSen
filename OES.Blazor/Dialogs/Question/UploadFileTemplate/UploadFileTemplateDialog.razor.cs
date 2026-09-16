using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.QuestionUploadTemplate;
using OES.Helper.Dtos.QuestionUploadTemplateDto.OES.Helper.Dtos.UploadFiles;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Dialogs.Question.UploadFileTemplate
{
    public partial class UploadFileTemplateDialog : ComponentBase
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Inject] IBlazQuestionUploadTemplateService BlazQuestionUploadTemplateService { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }

        private int listChangingKey;
        private readonly string[] CoulmnsNames = [nameof(QuestionUploadTemplateDto.Name)];

        private void SelectTemplate(QuestionUploadTemplateDto template)
        {
            MudDialog.Close(DialogResult.Ok(template));
        }

        private async Task DeleteTemplateAsync(object templateId)
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSure },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _ = long.TryParse(templateId.ToString(), out long parsedId);

                var response = await BlazQuestionUploadTemplateService.DeleteTemplateAsync(parsedId);

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
