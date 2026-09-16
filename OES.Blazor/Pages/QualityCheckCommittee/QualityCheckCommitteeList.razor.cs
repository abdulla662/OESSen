using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.QualityCheckCommittee;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.QualityCheckCommittee;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.QualityCheckCommittee
{
    public partial class QualityCheckCommitteeList
    {
        [Inject] IDialogService DialogService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] IBlazAuthService AuthService { get; set; }

        [Inject] IBlazQualityCheckCommitteeService BlazQualityCheckCommitteeService { get; set; }

        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }


        private async Task ViewCommittee(long committeeId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.QualityCheckCommitteeViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            if (committeeId <= 0)
            {
                Snackbar.Add(Resource.FailedToLoadCommitteeDetails, Severity.Error);
                return;
            }

            var parameters = new DialogParameters
            {
                { nameof(CommitteeDetailsDialog.CommitteeId), committeeId }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<CommitteeDetailsDialog>(
                Resource.CommitteeDetails,
                parameters,
                options);

            await dialog.Result;
        }

        private async Task NavigateToEditAsync(long committeeId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.QualityCheckCommitteeEditor);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(
                committeeId,
                MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/UpdateQualityCheckCommittee");
        }
    }
}
