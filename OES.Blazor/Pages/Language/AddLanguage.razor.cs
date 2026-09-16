using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Language;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Language
{
    public partial class AddLanguage
    {
        [Inject] IBlazLanguageService BlazLanguageService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }


        private LanguageCreateDto _model = new();

        private bool IsSubmitButtonDisabled =>
            string.IsNullOrEmpty(_model.Name) ||
            string.IsNullOrEmpty(_model.LanguageDirection);


        private async Task HandleValidSubmit()
        {
            var response = await BlazLanguageService.AddLanguage(_model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(Resource.LanguageAddedSuccessfully, Severity.Success);

                NavigationManager.NavigateTo("/Language");
            }
            else
            {
                Snackbar.Add(response.Message ?? Resource.ErrorAddingLanguage, Severity.Error);
            }
        }

        private void Cancel()
        {
            NavigationManager.NavigateTo("/Language");
        }
    }
}
