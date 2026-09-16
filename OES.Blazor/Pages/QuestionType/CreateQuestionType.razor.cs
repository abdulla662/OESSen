using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.QuestionType
{
    public partial class CreateQuestionType : ComponentBase
    {
        [Inject] IBLazQuestionType BLazQuestionType { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }

        private QuestionTypeDto model = new();

        public async Task OnValidSubmitAsync()
        {
            var response = await BLazQuestionType.CreateQuestionType(model);

            if (response != null)
            {
                Snackbar.Add(Resource.QuestionTypeCreatedSuccessfully, Severity.Success);

                NavigationManager.NavigateTo("/QuestionType");
            }
            else
            {
                Snackbar.Add(Resource.FailedToCreateQuestionType, Severity.Error);
            }
        }

        private void GoBack()
        {
            NavigationManager.NavigateTo("/");
        }
    }
}
