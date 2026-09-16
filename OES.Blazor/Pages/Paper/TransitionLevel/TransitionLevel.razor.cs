using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.TransitionLevel;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper.Transition;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Paper.TransitionLevel
{
    public partial class TransitionLevel : ComponentBase
    {
        [Inject] IDialogService DialogService { get; set; }

        [Inject] IBlazTransitionLevelService BlazTransitionLevel { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] IBlazAuthService AuthService { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

        private int LevelKey;

        private async Task DeleteTransitionLevelAsync(long transitionLevelId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.TransitionLevelDelete);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisItem },
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

            if (!result.Canceled)
            {
                var response = await BlazTransitionLevel.SoftDeleteTransitionLevel(transitionLevelId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    LevelKey++;
                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }

        private async Task OpenViewDialogAsync(long transitionLevelId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.TransitionLevelViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<TransitionLevelViewDialog>
            {
                { x => x.TransitionLevelId, transitionLevelId }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            await DialogService.ShowAsync<TransitionLevelViewDialog>(string.Empty, parameters, options);
        }

        private async Task NavigateToEditAsync(long transitionLevelId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.TransitionLevelEdit);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(transitionLevelId, MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/UpdateTransitionLevel");
        }
    }
}
