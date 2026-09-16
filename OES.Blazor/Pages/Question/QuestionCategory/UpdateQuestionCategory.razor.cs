using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Question.QuestionCategory
{
    public partial class UpdateQuestionCategory : ComponentBase
    {
        [Inject] IBlazQuestionCategoryService BlazQuestionCategoryService { get; set; }
        [Inject] IBlazSessionStorageService SessionStorageService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }


        private QuestionCategoryDto model = new();


        protected override async Task OnInitializedAsync()
        {
            var Id = await SessionStorageService.GetValue<long>("PerformEditBtnClick");

            model = await BlazQuestionCategoryService.GetCategoryById(Id);
        }

        private bool AreAdaptiveSettingsValid()
        {
            var values = new decimal?[]
            {
                model.StandardDeviation,
                model.StandardError1,
                model.StandardError2,
                model.BaseValue1,
                model.BaseValue2
            };

            int filledCount = values.Count(v => v.HasValue);

            return filledCount == 0 || filledCount == 5;
        }

        private async Task OnValidSubmitAsync()
        {
            if (!AreAdaptiveSettingsValid())
            {
                Snackbar.Add(Resource.YouMustEnterAll5AdaptiveSettingsOrLeaveThemAll, Severity.Warning);
                return;
            }

            var response = await BlazQuestionCategoryService.UpdateCategory(model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigationManager.NavigateTo("/QuestionCategory");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void Cancel() => NavigationManager.NavigateTo("/QuestionCategory");
    }
}
