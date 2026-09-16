using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.DifficultyLevel;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.DifficultyLevel;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.General.ProtectedEntityHelper;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.DifficultyLevel
{
    public partial class DifficultyLevelList : ComponentBase
    {
        [Inject] IDialogService DialogService { get; set; }

        [Inject] IBlazDifficultyLevelService BlazDifficultyLevelService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] IBlazAuthService AuthService { get; set; }

        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] GlobalUserContext GlobalUserContext { get; set; }

        private int _difficultyLevelsListChangingKey;

        private readonly string[] _displayColumnNames =
        [
            nameof(DifficultyLevelDto.Name),
            nameof(DifficultyLevelDto.FromDeltaFormatted),
            nameof(DifficultyLevelDto.ToDeltaFormatted),
            nameof(DifficultyLevelDto.DifficultyProfileName),
            nameof(DifficultyLevelDto.DifficultyProfileDescription),
            nameof(DifficultyLevelDto.DeltaTypeName)
        ];

        private async Task UpdateDifficultyLevelAsync(long difficultyLevelId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DifficultyLevelEditor);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(difficultyLevelId, MiscConstants.PerformEditBtnClick);
            NavigationManager.NavigateTo("/UpdateDifficultyLevel");
        }

        private async Task ViewDifficultyLevelDetailsAsync(long difficultyLevelId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DifficultyLevelViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var difficultyLevelModel = await BlazDifficultyLevelService.GetDifficultyLevelById(difficultyLevelId);

            var parameters = new DialogParameters<DifficultyLevelDetails>
            {
                { x => x.Model, difficultyLevelModel }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseOnEscapeKey = true
            };

            await DialogService.ShowAsync<DifficultyLevelDetails>(
                string.Empty,
                parameters,
                options
            );
        }

        private async Task DeleteDifficultyLevelAsync(long difficultyLevelId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DifficultyLevelDeleter);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeletethisDifficultyLevel },
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
                options
            );

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazDifficultyLevelService.DeleteDifficultyLevel(difficultyLevelId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _difficultyLevelsListChangingKey--;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }

        private bool IsEditDisabled(DifficultyLevelDto item)
        {
            return ProtectedEntityHelper.IsProtected(item.CreationUser, GlobalUserContext);
        }

        private bool IsDeleteDisabled(DifficultyLevelDto item)
        {
            return ProtectedEntityHelper.IsProtected(item.CreationUser, GlobalUserContext);
        }
    }
}
