using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Language;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Language
{
    public partial class EditLanguage
    {
        [Inject] IBlazLanguageService BlazLanguageService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] IBlazSessionStorageService _SessoinStorage { set; get; }


        private LanguageUpdateDto _model = new();

        private bool IsSubmitButtonDisabled =>
           string.IsNullOrEmpty(_model.Name) ||
           string.IsNullOrEmpty(_model.LanguageDirection);


        protected override async Task OnInitializedAsync()
        {
            var id = await _SessoinStorage.GetValue<long>("PerformEditBtnClick");

            var response = await BlazLanguageService.GetLanguageByIdAsync(id);

            if (response != null)
            {
                _model = new LanguageUpdateDto
                {
                    Id = response.Id,
                    Name = response.Name,
                    LanguageDirection = response.LanguageDirection
                };
            }
            else
            {
                Snackbar.Add(Resource.LanguageNotFound, Severity.Error);

                NavigationManager.NavigateTo("/Language");
            }
        }

        private async Task HandleValidSubmit()
        {
            var response = await BlazLanguageService.UpdateLanguageAsync(_model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(Resource.LanguageUpdatedSuccessfully, Severity.Success);

                NavigationManager.NavigateTo("/Language");
            }
            else
            {
                Snackbar.Add(response.Message ?? Resource.ErrorUpdatingLanguage, Severity.Error);
            }
        }

        private void Cancel()
        {
            NavigationManager.NavigateTo("/Language");
        }
    }
}