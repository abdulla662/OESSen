using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.PaperSetting.Responses;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Dialogs.Schedule
{
    public partial class SchedulePaperSettingsTemplatesDialog
    {
        [Inject] private IBlazPaperSettingsService BlazPaperSettingsService { get; set; }

        [Inject] private IDialogService DialogService { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

        [Parameter] public EventCallback<long> OnTemplateSelected { get; set; }

        private int listChangingKey;

        private void GetSelectedPaperSettingsTemplate(PaperSettingsTemplatePaginated selectedTemplate)
        {
            long templateId = selectedTemplate.Id;

            OnTemplateSelected.InvokeAsync(templateId);

            MudDialog.Close(DialogResult.Ok(templateId));
        }

        private async Task DeletePaperSettingsTemplateAsync(object templateId)
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
                _ = long.TryParse(templateId.ToString(), out long parsedId);

                var response = await BlazPaperSettingsService.DeletePaperSettingsTemplateAsync(parsedId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    listChangingKey--;

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