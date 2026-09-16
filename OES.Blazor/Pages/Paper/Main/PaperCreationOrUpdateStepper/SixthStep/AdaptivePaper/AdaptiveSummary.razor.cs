using Microsoft.AspNetCore.Components;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.Paper.Responses.AdaptiveSummary;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.SixthStep.AdaptivePaper
{
    public partial class AdaptiveSummary : ComponentBase
    {
        [Inject] IBlazPaperSummaryService BlazPaperSummaryService { get; set; }

        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        private int TotalBlockCount { get; set; }
        private string DurationDisplay { get; set; } = "-";
        private string LanguageDisplay { get; set; } = "-";
        private string SelectionTypeDisplay { get; set; } = "-";
        private string PaperTypeDisplay { get; set; } = "-";
        private string PaperSubTypeDisplay { get; set; } = "-";
        private string TotalMarkDisplay { get; set; } = "-";

        private PaperStageSummaryResponseDto summaryDto = new();

        private List<BlockFlatDto> flatBlocks = [];


        protected override async Task OnInitializedAsync()
        {
            var response = await BlazPaperSummaryService.GetAdaptiveSectionSummary(PaperMetadataResultedParamsDto.PaperId, PaperMetadataResultedParamsDto.SelectedQuestionSelectionType, PaperMetadataResultedParamsDto.SelectedPaperType);

            if (response.StatusCode == HttpStatusCode.OK && response.Data is PaperStageSummaryResponseDto dto)
            {
                summaryDto = dto;

                flatBlocks = summaryDto.Stages?
                    .SelectMany(stage => stage.Sections
                    .SelectMany(section => section.Blocks
                    .Select(block => new BlockFlatDto
                    {
                        StageName = stage.StageName,
                        StageTime = stage.StageTime,
                        SectionName = section.SectionName,
                        BlockName = block.BlockName,
                        BlockCode = block.BlockCode,
                        QuestionCount = block.QuestionCount
                    })))
                    .ToList() ?? [];

                TotalBlockCount = flatBlocks.Count;
            }

            if (PaperMetadataResultedParamsDto != null)
            {
                DurationDisplay = PaperMetadataResultedParamsDto.PaperExamDuration > 0 ? $"{(int)Math.Round(PaperMetadataResultedParamsDto.PaperExamDuration)} {Resource.Min}" : "-";

                TotalMarkDisplay = PaperMetadataResultedParamsDto.TotalExamMark > 0 ? PaperMetadataResultedParamsDto.TotalExamMark.ToString() : "-";

                PaperTypeDisplay = PaperMetadataResultedParamsDto.SelectedPaperType.ToString();

                PaperSubTypeDisplay = PaperMetadataResultedParamsDto.AdaptiveSubtype.ToString();

                SelectionTypeDisplay = PaperMetadataResultedParamsDto.SelectedQuestionSelectionType.ToString();

                LanguageDisplay = !string.IsNullOrWhiteSpace(PaperMetadataResultedParamsDto.LanguageName) ? PaperMetadataResultedParamsDto.LanguageName : "-";
            }

            StateHasChanged();
        }
    }
}
