using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.DifficultyProfile;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.General.ProtectedEntityHelper;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.DifficultyProfile
{
    public partial class DifficultyProfile : ComponentBase
    {
        [Inject] IBlazDifficultyProfileService BlazDifficultyProfileService { get; set; }

        [Inject] IDialogService DialogService { get; set; }

        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] IBlazAuthService AuthService { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] GlobalUserContext GlobalUserContext { get; set; }

        private int profilesListKey;

        private async Task UpdateDifficultyProfileAsync(long difficultyProfileId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DifficultyProfileEditor);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(
                difficultyProfileId,
                MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/UpdateProfile");
        }

        private async Task ViewDifficultyProfileDetailsAsync(long profileId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DifficultyProfileViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var profileModel = await BlazDifficultyProfileService.GetProfileById(profileId);

            var parameters = new DialogParameters<DifficultyProfileDetails>
            {
                { x => x.Model, profileModel }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseOnEscapeKey = true,
            };

            await DialogService.ShowAsync<DifficultyProfileDetails>(
                string.Empty,
                parameters,
                options
            );
        }

        private async Task DeleteProfileAsync(long profileId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DifficultyProfileDeleter);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var prams = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreyousureyouwanttodeletethisDifficultyProfile },
                { x => x.SubmitText, Resource.Delete },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var opts = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(
                Resource.DeleteDifficultyProfile,
                prams,
                opts
            );

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazDifficultyProfileService.SoftDeleteDifficultyProfile(profileId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    profilesListKey--;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }

        private bool IsEditDisabled(DifficultyProfileDto item)
        {
            return ProtectedEntityHelper.IsProtected(item.CreationUser, GlobalUserContext);
        }

        private bool IsDeleteDisabled(DifficultyProfileDto item)
        {
            return ProtectedEntityHelper.IsProtected(item.CreationUser, GlobalUserContext);
        }
    }
}
