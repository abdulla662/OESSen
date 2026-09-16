using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.CBTSyncSetting;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.CBTSyncSettingService;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.CBTSyncSetting.Responses;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.CBTSyncSetting
{
    public partial class CBTSyncSettingList : ComponentBase
    {
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazCBTSyncSettingService BlazCBTSyncSettingService { get; set; }
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] IBlazAuthService AuthService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        private int _listChangingKey;

        private readonly string[] _displayColumnNames =
        [
            nameof(CBTSyncSettingResponseDto.CBTAutoSyncEnabled),
            nameof(CBTSyncSettingResponseDto.CBTSyncScheduleTimeFormatted)
        ];

        private async Task ViewAsync(long id)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.CbtAutoSettingsViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<ViewCBTSyncSettingDialog>
            {
                { x => x.CBTSyncSettingId, id }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            await DialogService.ShowAsync<ViewCBTSyncSettingDialog>(Resource.Details, parameters, options);
        }

        private async Task UpdateAsync(long id)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.CbtAutoSettingsEditor);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(id, MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/UpdateCBTSyncSetting");
        }
    }
}
