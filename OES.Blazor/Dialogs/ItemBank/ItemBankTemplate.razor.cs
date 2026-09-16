using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Helper.ResourceFiles;
using System.Net;
namespace OES.Blazor.Pages.AIItemBankGenerator.Dialogs
{
    public partial class ItemBankTemplate
    {
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public EventCallback<long> OnTemplateSelected { get; set; }
        [Parameter] public bool IsFromItemBankAI { get; set; } = true;

        private int listChangingKey;

        private void GetTemplateById(long templateId)
        {
            OnTemplateSelected.InvokeAsync(templateId);
            MudDialog.Close(DialogResult.Ok(templateId));
        }

        private async Task DeleteItemBankTemplateAsync(object templateId)
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
                _ = long.TryParse(templateId.ToString(), out long parsedTemplateId);

                var response = await BlazItemBankService.DeleteItemBankTemplateAsync(parsedTemplateId);
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