using Microsoft.AspNetCore.Components;

namespace OES.Blazor.Pages.AIItemBankGenerator.FourthStep
{
    public partial class AIItemBankCompleteStep
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        [Parameter] public EventCallback OnReset { get; set; }

        private void NavigateToItemBankTree()
        {
            NavigationManager.NavigateTo("/ItemBankList");
        }

        private async Task StartNewGeneration()
        {
            if (OnReset.HasDelegate)
            {
                await OnReset.InvokeAsync();
            }
            else
            {
                NavigationManager.NavigateTo("/AIItemBankGenerator", forceLoad: true);
            }
        }
    }
}
