using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Subject;
using OES.Helper.Dtos.Subject;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Question.QuestionSubject
{
    public partial class AddSubject : ComponentBase
    {
        [Inject] IBlazSubjectService BlazSubjectService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }


        private SubjectDto model = new();


        private async Task OnValidSubmitAsync()
        {
            var response = await BlazSubjectService.CreateSubject(model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(Resource.SubjectCreatedSuccessfully, Severity.Success);

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
