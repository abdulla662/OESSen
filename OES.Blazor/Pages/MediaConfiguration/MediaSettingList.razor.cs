using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.MediaConfiguration;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.MediaSettingService;
using OES.Helper.Dtos.MediaSetting.Responses;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.MediaConfiguration
{
    public partial class MediaSettingList : ComponentBase
    {
        [Inject] IDialogService DialogService { get; set; }

        [Inject] IBlazMediaSettingService BlazMediaSettingService { get; set; }

        [Inject] IBlazAuthService AuthService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] GlobalUserContext GlobalUserContext { get; set; }


        private int _mediaSettingsListChangingKey;

        private readonly string[] _displayColumnNames =
        [
            nameof(MediaSettingResponseDto.MediaCategory),
            nameof(MediaSettingResponseDto.MaxSizeInKB)
        ];

        private async Task ViewMediaSettingAsync(long mediaSettingId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.MediaConfigurationViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<ViewMediaSettingDialog>
            {
                { x => x.MediaSettingId, mediaSettingId }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            await DialogService.ShowAsync<ViewMediaSettingDialog>(
                Resource.Details,
                parameters,
                options);
        }

        private async Task UpdateMediaSettingAsync(long mediaSettingId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.MediaConfigurationEditor);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(
                mediaSettingId,
                MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/UpdateMediaSetting");
        }

        private async Task DeleteMediaSettingAsync(long id)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.MediaConfigurationDeleter);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.Alert },
                { x => x.Content, Resource.MediaDeleteConfirmation },
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
                var response = await BlazMediaSettingService.DeleteAsync(id);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _mediaSettingsListChangingKey--;

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
