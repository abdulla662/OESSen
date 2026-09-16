using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.EquationTemplate.EquationTemplateViewDialog;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.EquationTemplate;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.EquationTemplate
{
    public partial class EquationTemplateList : ComponentBase
    {
        [Inject] private IBlazEquationTemplateService BlazEquationTemplateService { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IBlazAuthService AuthService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorage { get; set; }

        public GetEquationTemplateResponseDto Model { get; set; } = new GetEquationTemplateResponseDto();

        private int key;

        private async Task OpenViewDialog(long id)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.EquationTempelateViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            if (id > 0)
            {
                await BlazSessionStorage.SetValue(
                    MiscConstants.PerformViewBtnClick,
                    id);

                var parameters = new DialogParameters<EquationTemplateViewDialog>();

                var options = new DialogOptions
                {
                    CloseButton = true,
                    MaxWidth = MaxWidth.Medium,
                    FullWidth = true
                };

                await DialogService.ShowAsync<EquationTemplateViewDialog>(
                    Resource.EquationTemplateDetails,
                    parameters,
                    options);
            }
        }

        private async Task NavigateToEditAsync(long id)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.EquationEditor);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorage.SetCrudSessionAsync(
                id,
                MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/UpdateEquationTemplate");
        }

        private async Task<bool> ConfirmChangeAsync(string message)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, message },
                { x => x.SubmitText, Resource.Delete },
                { x => x.CancelText, Resource.Cancel },
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

            return !result.Canceled;
        }

        private async Task OpenDeleteDialog(object id)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.EquationDeleter);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            if (id is long parsedId)
            {
                var response = await BlazEquationTemplateService
                    .GetEquationTemplateById(parsedId);

                if (response?.StatusCode == HttpStatusCode.OK &&
                    response.Data != null)
                {
                    var equationTemplate =
                        (GetEquationTemplateResponseDto)response.Data;

                    if (await ConfirmChangeAsync(
                        string.Format(
                            Resource.ConfirmDeleteEquationTemplate,
                            equationTemplate.Name)))
                    {
                        var deleteResponse = await BlazEquationTemplateService
                            .DeleteEquationTemplateAsync(parsedId);

                        if (deleteResponse != null &&
                            deleteResponse.StatusCode == HttpStatusCode.OK)
                        {
                            key++;

                            Snackbar.Add(
                                string.Format(
                                    Resource.ThisEquationTemplateDeletedSuccessfully,
                                    equationTemplate.Name),
                                Severity.Success);
                        }
                        else
                        {
                            Snackbar.Add(
                                deleteResponse?.Message ??
                                Resource.FailedToDeleteEquationTemplate,
                                Severity.Error);
                        }
                    }
                }
                else
                {
                    Snackbar.Add(
                        Resource.FailedToFetchQuestionTypedetails,
                        Severity.Error);
                }
            }
            else
            {
                Snackbar.Add(
                    Resource.FailedToFetchQuestionTypedetails,
                    Severity.Error);
            }
        }
    }
}