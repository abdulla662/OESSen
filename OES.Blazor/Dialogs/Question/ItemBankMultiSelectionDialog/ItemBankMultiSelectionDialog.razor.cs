using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.GRPC;
using OES.Helper.Dtos.TreeItem;

namespace OES.Blazor.Dialogs.Question.ItemBankMultiSelectionDialog
{
    public partial class ItemBankMultiSelectionDialog
    {
        [Inject] private IItemBankTreeGrpcService ItemBankTreeGrpcService { get; set; }


        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

        [Parameter] public long ItemBankRootId { get; set; }

        [Parameter] public List<long> SelectedItemBankNodesIds { get; set; } = [];

        [Parameter] public bool EnableCascadingSelection { get; set; } = false;


        private List<TreeItemResponseDto> Items { get; set; } = [];


        private List<TreeItemResponseDto> _selectedItemBankNodes = [];

        private int _treeViewKey;


        protected override async Task OnInitializedAsync()
        {
            Items = await ItemBankTreeGrpcService.GetAllNestedItemBanksWithParentAsync(ItemBankRootId);

            if (SelectedItemBankNodesIds?.Count > 0)
            {
                _selectedItemBankNodes = Items.Where(x => SelectedItemBankNodesIds.Contains(x.Id)).ToList();
            }

            _treeViewKey--;
        }

        private void GetSelectedTreeItems(List<TreeItemResponseDto> treeItems)
        {
            _selectedItemBankNodes = treeItems;
        }

        private void Submit()
        {
            MudDialog.Close(DialogResult.Ok(_selectedItemBankNodes));
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
