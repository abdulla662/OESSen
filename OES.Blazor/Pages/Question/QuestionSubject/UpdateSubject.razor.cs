using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Subject;
using OES.Helper.Dtos.Subject;
using System.Net;

namespace OES.Blazor.Pages.Question.QuestionSubject
{
    public partial class UpdateSubject : ComponentBase
    {
        [Inject] IBlazSubjectService BlazSubjectService { get; set; }
        [Inject] IBlazSessionStorageService SessionStorageService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        private SubjectDto model = new();


        protected override async Task OnInitializedAsync()
        {
            var id = await SessionStorageService.GetValue<long>("PerformEditBtnClick");

            model = await BlazSubjectService.GetSubjectById(id);
        }

        private async Task OnValidSubmitAsync()
        {
            var response = await BlazSubjectService.UpdateSubject(model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigationManager.NavigateTo("/SubjectList");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void Cancel() => NavigationManager.NavigateTo("/SubjectList");
    }
}
