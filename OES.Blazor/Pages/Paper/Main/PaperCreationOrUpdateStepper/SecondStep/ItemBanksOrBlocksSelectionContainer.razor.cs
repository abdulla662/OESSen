using Microsoft.AspNetCore.Components;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.SecondStep.ItemBanksSelection;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.SecondStep
{
    public partial class ItemBanksOrBlocksSelectionContainer
    {
        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        public ItemBanksList ItemBanksSelectionComponentRef { get; set; }

        public BlocksSelection.BlocksSelection BlocksSelectionComponentRef { get; set; }
    }
}
