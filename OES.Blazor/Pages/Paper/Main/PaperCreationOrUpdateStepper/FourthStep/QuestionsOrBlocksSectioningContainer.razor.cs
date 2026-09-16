using Microsoft.AspNetCore.Components;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.FourthStep.BlockDistribution;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.FourthStep.QuestionsSectioning;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.FourthStep
{
    public partial class QuestionsOrBlocksSectioningContainer
    {
        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        public ManuallySelectedQuestionsSectioning ManuallySelectedQuestionsSectioningComponentRef { get; set; }

        public AutoSelectedQuestionsSectioning AutoSelectedQuestionsSectioningComponentRef { get; set; }

        public MSTBlockDistributionDropZone MSTBlocksDistributionDropZoneComponentRef { get; set; }

        public STEPBlockDistributionDropZone STEPBlocksDistributionDropZoneComponentRef { get; set; }
    }
}
