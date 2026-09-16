using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.DuplicateGenericTempleateDialog;
using OES.Blazor.Dialogs.GroupWizard;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.GroupsTempleates;
using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.RuleMatrix
{
    public partial class GroupWizardList
    {
        [Inject] IBlazOesRoleTemplateService TemplateService { get; set; } = default!;
        [Inject] IBlazGroupService GroupService { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;
        [Inject] NavigationManager Navigation { get; set; } = default!;


        private int _key = 0;

        public static string[] DisplayColumns =>
        [
            nameof(CreateTemplateDto.Name),
            nameof(CreateTemplateDto.Description),
        ];

        private void GoToCreatePage() => Navigation.NavigateTo("/GroupWizardCreate");

        private async Task OnEditClickAsync(object id)
        {
            if (!Guid.TryParse(id?.ToString(), out var groupId)) return;

            var parameters = new DialogParameters
            {
                { nameof(GroupCreationWizard.GroupId), groupId }
            };

            var dialog = await DialogService.ShowAsync<GroupCreationWizard>(
                Resource.EditTemplate, parameters,
                new DialogOptions { CloseButton = true, FullWidth = true, MaxWidth = MaxWidth.Large });

            var result = await dialog.Result;
            if (!result.Canceled)
            {
                Snackbar.Add(Resource.TemplateUpdatedSuccessfully, Severity.Success);
                _key++;
            }
        }

        private async Task DeleteAsync(object templateId)
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title,                 Resource.ConfirmDelete },
                { x => x.Content,               Resource.AreYouSureYouWantToDeleteThisGroup },
                { x => x.CancelText,            Resource.Cancel },
                { x => x.SubmitText,            Resource.Delete },
                { x => x.SubmitButtonColor,     Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(
                string.Empty,
                parameters,
                new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true });

            if ((await dialog.Result).Canceled) return;

            if (!Guid.TryParse(templateId?.ToString(), out var id) || id == Guid.Empty)
            {
                Snackbar.Add(Resource.InvalidTemplateId, Severity.Error);
                return;
            }

            var tpl = await TemplateService.GetTemplateDetailsAsync(id);
            if (tpl is null || tpl.Id == Guid.Empty)
            {
                Snackbar.Add(Resource.TemplateNotFound, Severity.Warning);
                return;
            }

            if (tpl.IsPredefined)
            {
                Snackbar.Add(Resource.ThisIsPreDefinedTemplate, Severity.Warning);
                return;
            }

            var response = await GroupService.DeleteGroupAsync(id);
            if (response?.StatusCode == HttpStatusCode.OK)
            {
                _key++;
                Snackbar.Add(response.Message ?? Resource.TemplateDeletedSuccessfully, Severity.Success);
            }
            else
            {
                Snackbar.Add(response?.Message ?? Resource.AnUnExpectedErrorOccurred, Severity.Error);
            }
        }

        private async Task DuplicateAsync(object templateId)
        {
            _ = Guid.TryParse(templateId.ToString(), out var id);

            // TODO: Use the unified way to pass parameters to the dialog, instead of using the dictionary way. (This is a temporary solution) (by Abd-Allah)
            var dialog = await DialogService.ShowAsync<DuplicateGenericTemplateDialog>(
                string.Empty,
                new DialogParameters { ["TemplateId"] = id },
                new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true });

            if (!(await dialog.Result).Canceled) _key++;
        }

        private Task<CustomTableData<CreateTemplateDto>> FetchAsync(PaginationSearchModel pagination)
            => TemplateService.GetAllRolesAndTemplate(pagination, false, ResourceType.All);
    }
}