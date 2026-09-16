using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Question.QuestionCategory;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Question.QuestionCategory
{
    public partial class QuestionCategory : ComponentBase
    {
        [Inject] IBlazQuestionCategoryService BlazQuestionCategoryService { get; set; }
        [Inject] IBlazAuthService AuthService { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        private int KeyCategory;

        private async Task ViewCategoryDetailsAsync(long categoryId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.QuestionCategoryViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var detailsModel = await BlazQuestionCategoryService
                .GetCategoryById(categoryId);

            var parameters = new DialogParameters<QuestionCategoryDetails>
            {
                { x => x.Model, detailsModel }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseOnEscapeKey = true
            };

            await DialogService.ShowAsync<QuestionCategoryDetails>(
                Resource.CategoryDetails,
                parameters,
                options);
        }

        private async Task NavigateToEditAsync(long categoryId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.QuestionCategoryEditor);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(
                categoryId,
                MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/UpdateCategory");
        }

        private async Task DeleteCategoryAsync(long categoryId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.QuestionCategoryDeleter);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisQuestionCategory },
                { x => x.SubmitText, Resource.Delete },
                { x => x.CancelText, Resource.Cancel },
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
                Resource.DeleteQuestionCategory,
                parameters,
                options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazQuestionCategoryService
                    .SoftDeleteCategory(categoryId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    KeyCategory--;

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
