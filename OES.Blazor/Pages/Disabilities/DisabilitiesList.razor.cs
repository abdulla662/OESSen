using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Disability;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Disabilities
{
    public partial class DisabilitiesList
    {
        [Inject] IBlazDisabilityService DisabilityService { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazAuthService AuthService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IBlazSessionStorageService ISessoinStorage { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        private int key = 0;

        private async Task EditDisabilityAsync(object id)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.SpecialCasesForAddingExtraTimeEditor);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await ISessoinStorage.SetValue("PerformEditBtnClick", (long)id);
            NavigationManager.NavigateTo("/UpdateDisability");
        }

        private async Task DeleteDisabilityAsync(object id)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.SpecialCasesForAddingExtraTimeDeleter);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeletethisDisability },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await DisabilityService.DeleteDisability((long)id);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    key--;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }
    }
}
