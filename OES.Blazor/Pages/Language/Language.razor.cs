using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Language;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Language
{
    public partial class Language
    {
        [Inject] IDialogService DialogService { get; set; }

        [Inject] IBlazLanguageService BlazLanguageService { get; set; }

        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] IBlazAuthService AuthService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] GlobalUserContext GlobalUserContext { get; set; }

        private int languagesListKey;

        private async Task UpdateLanguageAsync(long languageId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DefinedLanguageEditor);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(
                languageId,
                MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/EditLanguage");
        }

        private async Task DeleteLanguageAsync(long languageId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DefinedLanguageDeleter);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisLanguage },
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
                var response = await BlazLanguageService
                    .SoftDeleteLanguage(languageId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    languagesListKey--;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }
            }

            StateHasChanged();
        }
    }
}