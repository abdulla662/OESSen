using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.CandidatesResult;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.CandidatesResult;
using OES.Helper.Dtos.CandidatesResult;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using SharedHelper.RolesNames;

namespace OES.Blazor.Pages.CandidatesResult
{
    public partial class CandidatesResultList : ComponentBase
    {
        [Inject] private IBlazCandidatesResultService BlazCandidatesResultService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazAuthService AuthService { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private GlobalUserContext GlobalUserContext { get; set; }

        private bool IsCurrentUserSuperAdminOrEntityAdmin =>
            GlobalUserContext.SsoUserRoles.Exists(x => x.Name == AdminRoles.SuperAdmin || x.Name == AdminRoles.Entity_Admin);

        private async Task CandidatesResultViewAsync(long candidateId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.CandidateResultView);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            if (candidateId == 0)
            {
                Snackbar.Add(Resource.FailedToLoadCandidateDetails, Severity.Error);
                return;
            }

            var response = await BlazCandidatesResultService.GetCandidateResultByIdAsync(candidateId);

            if (response == null || response.Data == null)
            {
                Snackbar.Add(Resource.CandidateNotFound, Severity.Error);
                return;
            }

            var candidateResult = (GetCandidateQuestionsAnswersDto)response.Data;

            var parameters = new DialogParameters<CandidatesResultViewDialog>
    {
        { x => x.Model, candidateResult }
    };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<CandidatesResultViewDialog>(string.Empty, parameters, options);

            await dialog.Result;
        }
    }
}
