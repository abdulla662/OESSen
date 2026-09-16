using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.Paper.PaperSummary;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.SixthStep.StandardPaper
{
    public partial class StandardSummary
    {
        [Inject] IDialogService DialogService { get; set; }

        [Inject] IBlazPaperSummaryService BlazPaperSummaryService { get; set; }

        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        private string DurationDisplay { get; set; } = "-";
        private string LanguageDisplay { get; set; } = "-";
        private string SelectionTypeDisplay { get; set; } = "-";
        private string PaperTypeDisplay { get; set; } = "-";
        private string TotalMarkDisplay { get; set; } = "-";

        private bool IsStandardAutoPaper =>
            PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Standard && PaperMetadataResultedParamsDto.SelectedQuestionSelectionType == QuestionSelectionType.Auto;
        private bool IsStandardManualPaper =>
            PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Standard && PaperMetadataResultedParamsDto.SelectedQuestionSelectionType == QuestionSelectionType.Manual;


        private StandardManualPaperSummaryResponseDto standardManualPaperSummaryDto = new();

        private StandardAutoPaperSummaryResponseDto standardAutoPaperSummaryDto = new();


        protected async override void OnInitialized()
        {
            if (IsStandardManualPaper)
            {
                var response = await BlazPaperSummaryService.GetStandardSectionSummaryAsync<StandardManualPaperSummaryResponseDto>(PaperMetadataResultedParamsDto.PaperId, PaperMetadataResultedParamsDto.SelectedQuestionSelectionType, PaperMetadataResultedParamsDto.SelectedPaperType);

                standardManualPaperSummaryDto = (StandardManualPaperSummaryResponseDto)response.Data ?? new();
            }
            else if (IsStandardAutoPaper)
            {
                var response = await BlazPaperSummaryService.GetStandardSectionSummaryAsync<StandardAutoPaperSummaryResponseDto>(PaperMetadataResultedParamsDto.PaperId, PaperMetadataResultedParamsDto.SelectedQuestionSelectionType, PaperMetadataResultedParamsDto.SelectedPaperType);

                standardAutoPaperSummaryDto = (StandardAutoPaperSummaryResponseDto)response.Data ?? new();
            }

            if (PaperMetadataResultedParamsDto != null)
            {
                TotalMarkDisplay = PaperMetadataResultedParamsDto.TotalExamMark > 0 ? PaperMetadataResultedParamsDto.TotalExamMark.ToString() : "-";

                DurationDisplay = PaperMetadataResultedParamsDto.PaperExamDuration > 0 ? $"{(int)Math.Round(PaperMetadataResultedParamsDto.PaperExamDuration)} {Resource.Min}" : "-";

                LanguageDisplay = !string.IsNullOrWhiteSpace(PaperMetadataResultedParamsDto.LanguageName) ? PaperMetadataResultedParamsDto.LanguageName : "-";

                SelectionTypeDisplay = PaperMetadataResultedParamsDto.SelectedQuestionSelectionType.ToString();

                PaperTypeDisplay = PaperMetadataResultedParamsDto.SelectedPaperType.ToString();
            }

            StateHasChanged();
        }

        private void ShowItembankDetails(SectionItemBankSummaryResponseDto itembank)
        {
            var parameters = new DialogParameters<SummaryDialog>
            {
                { x => x.ItemBank, itembank }
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                BackdropClick = false
            };

            DialogService.Show<SummaryDialog>("", parameters, options);
        }
    }
}
