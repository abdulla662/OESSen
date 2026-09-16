using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.Paper.Responses.AdaptiveSummary;
using OES.Helper.Enums;
using SharedHelper.Enums;
using System.Net;

namespace OES.Blazor.Dialogs.Paper
{
    public partial class ViewAdaptivePaperDialog
    {
        [Inject] IBlazPaperSummaryService BlazPaperSummaryService { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }
        [Parameter] public long PaperId { get; set; }
        [Parameter] public string PaperName { get; set; }
        [Parameter] public string PaperTypeDisplay { get; set; }
        [Parameter] public string SubTypeDisplay { get; set; }
        [Parameter] public PaperType PaperType { get; set; }
        [Parameter] public string SubType { get; set; }


        private bool _isLoading = true;
        private PaperStageSummaryResponseDto _summaryDto;
        private List<BlockFlatDto> _flatBlocks = [];

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _ = Enum.TryParse(SubType, out QuestionSelectionType selectionType);

                var response = await BlazPaperSummaryService.GetAdaptiveSectionSummary(
                    PaperId,
                    selectionType,
                    PaperType
                );

                if (response.StatusCode == HttpStatusCode.OK && response.Data is PaperStageSummaryResponseDto dto)
                {
                    _summaryDto = dto;

                    if (_summaryDto.Stages != null)
                    {
                        _flatBlocks = [.. _summaryDto.Stages
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
                                    })
                                )
                            )
                        ];
                    }
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void Close() => MudDialog.Close();
    }
}
