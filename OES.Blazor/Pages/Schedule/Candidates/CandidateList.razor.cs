using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Candidate;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Candidate;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Notification;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Schedule.Candidates
{
    public partial class CandidateList
    {
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazCandidateService BlazCandidateService { get; set; }
        [Inject] private INotificationManager NotificationManager { get; set; }
        [Inject] private IBlazAuthService AuthService { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }

        private int Key { get; set; } = 0;

        protected override async Task OnInitializedAsync()
        {
            await NotificationManager.InitializeAsync();

            NotificationManager.OnNotificationReceived += NotificationReceived;
        }

        private void NotificationReceived(NotificationDto notification)
        {
            Key--;

            StateHasChanged();
        }

        private async Task UploadCandidatesAsync()
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.CandidateImport);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var dialogParams = new DialogParameters<CandidatesImportDialog>();

            var dialogOptions = new DialogOptions()
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                CloseButton = true
            };

            var dialog = await DialogService.ShowAsync<CandidatesImportDialog>(
                string.Empty,
                dialogParams,
                dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                Key--;
            }
        }

        private async Task ViewCandidateAsync(long candidateId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.CandidateViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var candidate = await BlazCandidateService.GetCandidateByIdAsync(candidateId);

            if (candidate == null)
            {
                Snackbar.Add(Resource.FailedToLoadCandidateDetails, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<CandidateViewDialog>
            {
                { x => x.Candidate, candidate }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<CandidateViewDialog>(
                Resource.CandidateDetails,
                parameters,
                options);

            await dialog.Result;
        }

        private async Task NavigateToEditAsync(long candidateId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.CandidateEditor);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(
                candidateId,
                MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/UpdateCandidate");
        }

        private async Task DeleteCandidateAsync(long candidateId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.CandidateDeleter);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.MessageDeleteCandidate },
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

            var dialog = await DialogService.ShowAsync<GenericDialog>(
                string.Empty,
                parameters,
                options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazCandidateService.DeleteCandidate(candidateId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Key--;

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