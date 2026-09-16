using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Dialogs.Paper.PaperTemplates
{
    public partial class PaperMetadataTemplate
    {
        [Inject] IBlazPaperService BlazPaperService { get; set; }

        [Inject] IDialogService DialogService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public EventCallback<long> OnPaperSelected { get; set; }

        private int listChangingKey;

        private void GetPaperMetadataById(long paperMetadataId)
        {
            OnPaperSelected.InvokeAsync(paperMetadataId);

            MudDialog.Close(DialogResult.Ok(paperMetadataId));
        }

        private async Task DeletePaperMetadataTemplateAsync(object paperMetadataTemplateId)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisTemplate },
                { x => x.SubmitText, Resource.Delete },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _ = long.TryParse(paperMetadataTemplateId.ToString(), out long parsedId);

                var response = await BlazPaperService.DeletePaperMetadataTemplateAsync(parsedId);

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

        private void Close() => MudDialog.Cancel();
    }
}

