using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Question.QuestionSubjectDetails;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Subject;
using OES.Helper.Dtos.Subject;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.General.ProtectedEntityHelper;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Question.QuestionSubject
{
    public partial class SubjectList : ComponentBase
    {
        [Inject] IBlazSubjectService BlazSubjectService { get; set; }

        [Inject] IDialogService DialogService { get; set; }

        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] IBlazAuthService AuthService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] GlobalUserContext GlobalUserContext { get; set; }

        private int subjectsListKey;

        private async Task ViewSubjectAsync(long subjectId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DefinedMaterialViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var subjectModel = await BlazSubjectService.GetSubjectById(subjectId);

            var parameters = new DialogParameters<QuestionSubjectDetails>
            {
                { x => x.Model, subjectModel }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseOnEscapeKey = true,
            };

            await DialogService.ShowAsync<QuestionSubjectDetails>(
                string.Empty,
                parameters,
                options);
        }

        private async Task UpdateSubjectAsync(long subjectId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DefinedMaterialEditor);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(
                subjectId,
                MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/UpdateSubject");
        }

        private async Task DeleteSubjectAsync(long subjectId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DefinedMaterialDeleter);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var prams = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisSubject },
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
                Resource.DeleteSubject,
                prams,
                options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazSubjectService.DeleteSubject(subjectId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    subjectsListKey--;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }

        private bool IsEditDisabled(SubjectDto item)
        {
            return ProtectedEntityHelper.IsProtected(item.CreationUser, GlobalUserContext);
        }

        private bool IsDeleteDisabled(SubjectDto item)
        {
            return ProtectedEntityHelper.IsProtected(item.CreationUser, GlobalUserContext);
        }
    }
}
