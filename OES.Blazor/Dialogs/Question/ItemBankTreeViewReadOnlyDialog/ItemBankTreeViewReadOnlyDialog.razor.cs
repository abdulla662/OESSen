using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TreeViewTemplates.GrpcTreeViewTemplate;
using OES.Blazor.Services.Interfaces.GRPC;
using OES.Helper.Dtos.TreeItem;

namespace OES.Blazor.Dialogs.Question.ItemBankTreeViewReadOnlyDialog
{
    public partial class ItemBankTreeViewReadOnlyDialog
    {
        [Inject] private IItemBankTreeGrpcService ItemBankTreeGrpcService { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

        [Parameter] public long ItemBankRootId { get; set; }

        [Parameter] public long SelectedItemBankNodeId { get; set; }

        private List<TreeItemResponseDto> Items { get; set; } = [];

        private bool IsInitialized { get; set; }

        private int _treeViewKey;

        // Read-only mode — no buttons shown
        private static TreeCrudButtonsRolesPlaceholders TreeCrudButtonsRolesPlaceholders => new(string.Empty, SingleSelectionRole: string.Empty);

        protected override async Task OnInitializedAsync()
        {
            var rootId = ItemBankRootId > 0 ? ItemBankRootId : SelectedItemBankNodeId;

            var fetchedItems = await ItemBankTreeGrpcService.GetAllNestedItemBanksWithParentAsync(rootId);

            Items.AddRange(fetchedItems);

            _treeViewKey--;

            IsInitialized = true;
        }

        private void Close() => MudDialog.Cancel();
    }
}
