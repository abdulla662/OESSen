using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.ItemBank
{
    public partial class ItemBankList : ComponentBase
    {
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private GlobalUserContext GlobalUserContext { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorage { get; set; }
        [Inject] private IBlazAuthService AuthService { get; set; }

        private int listItemReloadingKey;


        private async Task ViewItemBankAsync(long itemBankId)
        {
            var userAuthorized = await AuthService.IsCurrentUserAuthorizedAsync(
                itemBankId,
                OesTemplateRoleConstants.ItemBankViewer,
                BlazItemBankService.GetItemBankGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!userAuthorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToViewItemBank, Severity.Error);
                return;
            }

            await BlazSessionStorage.SetCrudSessionAsync(itemBankId, MiscConstants.PerformViewBtnClick);

            NavigationManager.NavigateTo("/itemBankTreeView");
        }

        private async Task UpdateItemBankAsync(long itemBankId)
        {
            var userAuthorized = await AuthService.IsCurrentUserAuthorizedAsync(
                itemBankId,
                OesTemplateRoleConstants.ItemBankEditor,
                BlazItemBankService.GetItemBankGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!userAuthorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToUpdateItemBank, Severity.Error);
                return;
            }

            await BlazSessionStorage.SetCrudSessionAsync(itemBankId, MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/Update");
        }

        private async Task RemoveRootItemBankAsync(long itemBankId)
        {
            var userAuthorized = await AuthService.IsCurrentUserAuthorizedAsync(
                itemBankId,
                OesTemplateRoleConstants.ItemBankDeleter,
                BlazItemBankService.GetItemBankGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!userAuthorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToDeleteItemBank, Severity.Error);
                return;
            }

            var deserializedResponse = await BlazItemBankService.CanSoftDeleteItemBankAsync(itemBankId);

            if (deserializedResponse.StatusCode == HttpStatusCode.OK)
            {
                var parameters = new DialogParameters<GenericDialog>
                {
                    { p => p.Title, Resource.ConfirmDelete },
                    { p => p.Content, Resource.ItemBankDeletionConfirmation },
                    { p => p.SubmitText, Resource.Delete },
                    { p => p.CancelText, Resource.Cancel },
                    { p => p.SubmitButtonColor, Color.Error },
                    { p => p.SubmitButtonStartIcon, Icons.Material.Filled.Delete },
                };

                var options = new DialogOptions
                {
                    CloseButton = true,
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true
                };

                var dialog = await DialogService.ShowAsync<GenericDialog>(Resource.Alert, parameters, options);

                var result = await dialog.Result;

                if (!result.Canceled)
                {
                    var response = await BlazItemBankService.ExecuteSoftDeleteForRootAsync(itemBankId);
                    listItemReloadingKey++;
                    Snackbar.Add(response.Message, Severity.Success);
                    StateHasChanged();
                }
            }
            else
            {
                Snackbar.Add((MarkupString)deserializedResponse.Message, Severity.Error);
            }
        }
    }
}
