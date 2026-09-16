using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Helper.Dtos.ItemBank;

namespace OES.Blazor.Pages.ItemBankTreeView.ItemBankStatisticsComponent
{
    public partial class ItemBankStatisticsComponent
    {
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; }

        [Parameter] public long ItemBankId { get; set; }


        private ItemBankStatisticsDto _itemBankStatisticsDto = new();


        protected override async Task OnInitializedAsync()
        {
            await LoadItemBankStatisticsAsync();
        }

        public async Task LoadItemBankStatisticsAsync()
        {
            _itemBankStatisticsDto = await BlazItemBankService.GetItemBankStatistics(ItemBankId);
        }
    }
}
