using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.GroupsTempleates;
using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Dialogs.LoadRolesTemplates
{
    public partial class LoadRolesTemplateDialog
    {
        [Inject] private IBlazOesRoleTemplateService TemplateService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [CascadingParameter] private MudDialogInstance DialogInstance { get; set; } = default!;
        [Parameter] public ResourceType ResourceType { get; set; }

        private int ListRefreshKey;

        // ==========================================
        // Select Template
        // ==========================================
        private async Task SelectTemplate(CreateTemplateDto selectedTemplate)
        {
            if (selectedTemplate == null)
            {
                Snackbar.Add(Resource.NoTemplateSelected, Severity.Error);
                return;
            }

            var loadedTemplate = await TemplateService.GetTemplateDetailsAsync(selectedTemplate.Id);

            if (loadedTemplate == null || loadedTemplate.Id == Guid.Empty)
            {
                Snackbar.Add(Resource.FailedToLoadTemplateDetails, Severity.Error);
                return;
            }

            DialogInstance.Close(DialogResult.Ok(loadedTemplate));
        }

        // ==========================================
        // Delete Template
        // ==========================================
        private async Task DeleteTemplateAsync(object templateId)
        {
            var confirmationDialog = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.Delete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisTemplate },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete }
            };

            var dialogOptions = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, confirmationDialog, dialogOptions);
            var dialogResult = await dialog.Result;

            if (dialogResult.Canceled)
                return;

            _ = Guid.TryParse(templateId?.ToString(), out Guid parsedTemplateId);
            var deleteResponse = await TemplateService.DeleteTemplateAsync(parsedTemplateId);

            if (deleteResponse.StatusCode == HttpStatusCode.OK)
            {
                ListRefreshKey++;
                Snackbar.Add(deleteResponse.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(deleteResponse.Message, Severity.Error);
            }

            StateHasChanged();
        }
    }
}
