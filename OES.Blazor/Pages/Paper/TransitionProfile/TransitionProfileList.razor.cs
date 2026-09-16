using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Paper.TransitionProfiles;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.TransitionProfile;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Paper.TransitionProfile
{
    public partial class TransitionProfileList
    {
        [Inject] IDialogService DialogService { get; set; }

        [Inject] IBlazTransitionProfileService BlazTransitionProfile { get; set; }

        [Inject] IBlazAuthService AuthService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        private int _transactionKey;

        private async Task DeleteTransitionAsync(long profileId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.TransitionProfileDelete);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.ConfirmDelete},
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisItem },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(
                Resource.Delete,
                parameters,
                options
            );

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazTransitionProfile
                    .SoftDeleteTransitionProfileAsync(profileId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _transactionKey++;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);

                    StateHasChanged();
                }
            }
        }

        private async Task ViewTransitionProfileAsync(long profileId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.TransitionProfileViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<TransitionProfileViewDialog>
            {
                { d => d.ProfileId, profileId }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
            };

            var dialog = await DialogService.ShowAsync<TransitionProfileViewDialog>(
                string.Empty,
                parameters,
                options
            );

            await dialog.Result;
        }

        private async Task NavigateToEditAsync(long profileId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.TransitionProfileEdit);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(
                profileId,
                MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/EditTransitionProfile");
        }
    }
}
