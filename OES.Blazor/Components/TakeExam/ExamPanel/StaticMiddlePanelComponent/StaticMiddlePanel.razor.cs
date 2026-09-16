using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.QuestionMetaData;
using OES.Helper.Dtos.Question;
using OES.Helper.Enums;


namespace OES.Blazor.Components.TakeExam.ExamPanel.StaticMiddlePanelComponent
{
    public partial class StaticMiddlePanel
    {
        [Inject] IBlazQuestionMetaData BlazQuestionMetaData { get; set; } = default!;
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;

        [Parameter] public long QuestionMetaDataId { get; set; }

        private GetQuestionMetaDataForQCViewDto Model { get; set; } = new();
        private LayoutOrientation Orientation { get; set; } = LayoutOrientation.Horizontal;
        private Breakpoint SelectedScreenSize { get; set; } = Breakpoint.Md;
        private string QuestionType { get; set; } = string.Empty;
        private string SelectScreenSizeClass { get; set; } = "w-100";
        private bool IsInitialized { get; set; }


        protected override async Task OnInitializedAsync()
        {
            var id = await BlazSessionStorageService.GetValue<long>("MiddlePanel_QuestionId");

            if (id > 0)
                await LoadQuestionMetaData(id);

            SelectedScreenSize = Breakpoint.Md;
            SelectScreenSizeClass = "w-100";
            IsInitialized = true;
        }

        private async Task LoadQuestionMetaData(long questionMetaDataId)
        {
            var response = await BlazQuestionMetaData.GetQuestionMetaDataForQCView(questionMetaDataId);

            if (response?.Data == null)
            {
                Snackbar.Add(response.Message, Severity.Error);
                return;
            }

            Model = (GetQuestionMetaDataForQCViewDto)response.Data;
            QuestionType = Model?.QuestionTypeName ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(Model?.LayoutName) && Enum.TryParse<LayoutOrientation>(Model.LayoutName, out var parsedOrientation))
                Orientation = parsedOrientation;

            StateHasChanged();
        }
    }
}
