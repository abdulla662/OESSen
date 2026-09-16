using Microsoft.AspNetCore.Components;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.SixthStep.AdaptivePaper;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.SixthStep.StandardPaper;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.SixthStep
{
    public partial class PaperSummary
    {
        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        public AdaptiveSummary AdaptiveSummaryComponentRef { get; set; }

        public StandardSummary StandardSummaryComponentRef { get; set; }
    }
}
