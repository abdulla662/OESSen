using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper.Dtos;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.QuestionType
{
    public partial class QuestionTypeDetails : ComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] CRUD_Dto CrudDto { get; set; }
        [Inject] IBlazSessionStorageService BlazSessionStorage { set; get; }
        [Inject] IBLazQuestionType BLazQuestionTypeService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }

        private QuestionTypeDto model = new();

        protected override async Task OnInitializedAsync()
        {
            CrudDto.Id = await BlazSessionStorage.GetValue<long>("PerformViewBtnClick");

            var response = await BLazQuestionTypeService.GetQuestionType((long)CrudDto.Id);

            if (response.CustomCodeStatus == CustomCodeStatus.Success && response.Data is QuestionTypeDto questionTypeDto)
            {
                model = questionTypeDto;
            }
            else
            {
                Snackbar.Add(response.Message ?? Resource.FailedToFetchQuestionTypedetails, Severity.Error);
            }
        }

        private void Close()
        {
            NavigationManager.NavigateTo("/QuestionType");
        }
    }
}
