using Microsoft.AspNetCore.Components;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.ThirdStep.QuestionsSelection;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.ThirdStep
{
    public partial class QuestionsSelectionContainer
    {
        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        public ManualQuestionsSelection ManualQuestionsSelectionComponentRef { get; set; }
    }
}
