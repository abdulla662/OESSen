using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.QuestionType
{
    public partial class UpdateQuestionType : ComponentBase
    {
        [Inject] IBLazQuestionType BLazQuestionType { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        private QuestionTypeDto model = new();

        public async Task OnValidSubmitAsync()
        {
            var response = await BLazQuestionType.UpdateQuestionType(model);

            if (response != null)
            {
                Snackbar.Add(Resource.QuestionTypeUpdatedSuccessfully, Severity.Success);

                NavigationManager.NavigateTo("/QuestionType");
            }
            else
            {
                Snackbar.Add(Resource.FailedToUpdatedQuestionType, Severity.Error);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            var id = await BlazSessionStorageService.GetValue<long>("PerformEditBtnClick");

            var response = await BLazQuestionType.GetQuestionType(id);

            var EditModel = response.Data as QuestionTypeDto;

            model.Id = EditModel.Id;

            model.Name = EditModel.Name;
        }

        private void GoBack()
        {
            NavigationManager.NavigateTo("/");
        }
    }
}
