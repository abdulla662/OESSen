using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.QuestionLayout;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.Dtos.QuestionLayout;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.AIQuestionGenerator.Dialogs
{
    public partial class AIQuestionMetadataDialog
    {
        [Inject] private IBlazQuestionLayoutService BlazQuestionLayoutService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public AIQuestionMetadataDto QuestionMetadataDto { get; set; }

        private List<LayoutDto> FilteredQuestionOptions { get; set; } = [];
        private LayoutDto SelectedLayout { get; set; } = new();

        private string _code = string.Empty;


        protected override async Task OnInitializedAsync()
        {
            if (QuestionMetadataDto == null) return;

            _code = QuestionMetadataDto.Code;

            FilteredQuestionOptions = await BlazQuestionLayoutService.GetAllLayoutsByQuestionTypeId(QuestionMetadataDto.QuestionTypeId) ?? [];

            SelectedLayout = FilteredQuestionOptions.FirstOrDefault(x => x.Id == QuestionMetadataDto.LayoutId) ?? new LayoutDto();
        }

        public bool ValidateData()
        {
            if (SelectedLayout.Id == 0)
            {
                Snackbar.Add(Resource.CannotProceedWithoutASelectedLayout, Severity.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_code))
            {
                Snackbar.Add(Resource.ThisFieldIsRequired, Severity.Error);
                return false;
            }

            return true;
        }

        private void OnLayoutChange(LayoutDto selectedLayout)
        {
            SelectedLayout = selectedLayout;
        }

        public static LayoutOrientation GetLayoutOrientation(string name)
        {
            if (string.IsNullOrEmpty(name)) return LayoutOrientation.Vertical;

            if (Enum.TryParse<LayoutOrientation>(name, true, out var orientation))
            {
                return orientation;
            }

            return LayoutOrientation.Vertical;
        }

        private void Cancel()
        {
            MudDialog?.Cancel();
        }

        private void Submit()
        {
            if (!ValidateData()) return;

            QuestionMetadataDto!.Code = _code;
            QuestionMetadataDto.LayoutId = SelectedLayout.Id;
            QuestionMetadataDto.LayoutName = SelectedLayout.Name;

            MudDialog.Close(DialogResult.Ok(true));
        }
    }
}
