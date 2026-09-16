using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.CreateGroupTemplate;
using OES.Blazor.Dialogs.DuplicateGenericTempleateDialog;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.GroupsTempleates;
using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.RuleMatrix
{
    public partial class AllGroupsList
    {
        [Inject] IBlazOesRoleTemplateService IBlazOesTemplateService { get; set; }
        [Inject] IBlazGroupService GroupService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IDialogService DialogService { get; set; }

        private int key = 0;

        public static string[] DisplayColumns =>
        [
            nameof(CreateTemplateDto.Name),
            nameof(CreateTemplateDto.Description),
        ];

        private async Task DeleteTemplateAsync(object templateId)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisGroup },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);
            var result = await dialog.Result;

            if (result.Canceled)
                return;

            if (!Guid.TryParse(templateId?.ToString(), out Guid parsedId) || parsedId == Guid.Empty)
            {
                Snackbar.Add(@Resource.InvalidTemplateId, Severity.Error);
                return;
            }

            var templateDto = await IBlazOesTemplateService.GetTemplateDetailsAsync(parsedId);

            if (templateDto == null || templateDto.Id == Guid.Empty)
            {
                Snackbar.Add(Resource.TemplateNotFound, Severity.Warning);
                return;
            }

            if (templateDto.IsPredefined)
            {
                Snackbar.Add(@Resource.ThisIsPreDefinedTemplate, Severity.Warning);
                return;
            }

            var response = await GroupService.DeleteGroupAsync(parsedId);

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                key++;
                Snackbar.Add(response.Message ?? Resource.TemplateDeletedSuccessfully, Severity.Success);
            }
            else
            {
                Snackbar.Add(response?.Message ?? Resource.AnUnExpectedErrorOccurred, Severity.Error);
            }
        }

        private async Task DuplicateTemplateAsync(object templateId)
        {
            _ = Guid.TryParse(templateId.ToString(), out Guid parsedId);

            var parameters = new DialogParameters
            {
                ["TemplateId"] = parsedId
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<DuplicateGenericTemplateDialog>(
                string.Empty,
                parameters,
                options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                key++;
            }
        }

        private async Task OpenCreateTemplateDialogAsync()
        {
            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Large
            };

            var dialog = await DialogService.ShowAsync<CreateTemplateDialog>(Resource.CreateTemplate, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is CreateTemplateDto createdTemplate)
            {
                Snackbar.Add(
                    string.Format(Resource.TemplateCreatedSuccessfully, createdTemplate.Name),
                    Severity.Success
                );

                key++;
            }
        }

        private async Task OpenEditTemplateDialog(Guid templateId, ResourceType resource)
        {
            var parameters = new DialogParameters
            {
                { DialogParameterKeys.TemplateId, templateId },
                { DialogParameterKeys.ResourceType, resource },
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Large
            };

            var dialog = await DialogService.ShowAsync<EditRoleTemplate>(Resource.EditTemplate, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                Snackbar.Add(@Resource.TemplateUpdatedSuccessfully, Severity.Success);

                key++;
            }
        }

        private Task<CustomTableData<CreateTemplateDto>> FetchTemplatesAsync(PaginationSearchModel pagination)
            => IBlazOesTemplateService.GetAllRolesAndTemplate(pagination, false, ResourceType.All);

        private async Task OnEditClickAsync(object templateId)
            => await OpenEditTemplateDialog((Guid)templateId, ResourceType.All);
    }
}