using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ILOSection;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.ILOSection
{
    public partial class ILORoots : ComponentBase
    {
        [Inject] private IDialogService DialogService { get; set; }

        [Inject] private IBlazILOService BlazILOService { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; }

        [Inject] private NavigationManager NavigationManager { get; set; }

        [Inject] private GlobalUserContext GlobalUserContext { get; set; }

        [Inject] private IBlazSessionStorageService BlazSessionStorage { get; set; }

        [Inject] private IBlazAuthService AuthService { get; set; }

        private int iloListKey;


        private async Task UpdateIloAsync(long iloId)
        {
            var userAuthorized = await AuthService.IsCurrentUserAuthorizedAsync(
                iloId,
                OesTemplateRoleConstants.IloEditor,
                BlazILOService.GetIloGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!userAuthorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToUpdateIlo, Severity.Error);
                return;
            }

            await BlazSessionStorage.SetCrudSessionAsync(iloId, MiscConstants.PerformEditBtnClick);
            NavigationManager.NavigateTo("/EditIloRoot");
        }

        private async Task ViewIloAsync(long iloId)
        {
            var userAuthorized = await AuthService.IsCurrentUserAuthorizedAsync(
                iloId,
                OesTemplateRoleConstants.IloViewer,
                BlazILOService.GetIloGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!userAuthorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToViewIlo, Severity.Error);
                return;
            }

            await BlazSessionStorage.SetCrudSessionAsync(iloId, MiscConstants.PerformViewBtnClick);

            NavigationManager.NavigateTo("/iloTreeView");
        }

        private async Task DeleteIloAsync(long iloId)
        {
            var userAuthorized = await AuthService.IsCurrentUserAuthorizedAsync(
                iloId,
                OesTemplateRoleConstants.IloDeleter,
                BlazILOService.GetIloGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!userAuthorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToDeleteIlo, Severity.Error);
                return;
            }

            var canDeleteResponse = await BlazILOService.CanSoftDeleteIloRootAsync(iloId);

            if (canDeleteResponse.StatusCode != HttpStatusCode.Accepted)
            {
                Snackbar.Add(canDeleteResponse.Message, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.ConfirmDelete },
                { p => p.Content, Resource.WhenDeletingtheILO },
                { p => p.SubmitText, Resource.Delete },
                { p => p.CancelText, Resource.Cancel },
                { p => p.SubmitButtonColor, Color.Error },
                { p => p.SubmitButtonStartIcon, Icons.Material.Filled.Delete },
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<GenericDialog>(Resource.AreYouSure, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazILOService.SoftDeleteILORoot((long)iloId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    iloListKey++;

                    Snackbar.Add(Resource.ILOhasbeendeletedsuccessfully, Severity.Success);
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
