using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TreeViewTemplates.GrpcTreeViewTemplate;
using OES.Blazor.Services.Interfaces.GRPC;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;

namespace OES.Blazor.Dialogs.Question.ItemBankSelectionDialog
{
    public partial class ItemBankSelectionDialog
    {
        [Inject] private IItemBankTreeGrpcService ItemBankTreeGrpcService { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

        [Parameter] public long ItemBankRootId { get; set; }

        [Parameter] public long SelectedItemBankNodeId { get; set; }

        private List<TreeItemResponseDto> Items { get; set; } = [];

        private TreeItemResponseDto? SelectedTreeItem { get; set; }

        private static TreeCrudButtonsRolesPlaceholders TreeCrudButtonsRolesPlaceholders =>
           new("", SingleSelectionRole: OesTemplateRoleConstants.QuestionCreator);


        private int _treeViewKey;


        protected override async Task OnInitializedAsync()
        {
            var fetchedItems = await ItemBankTreeGrpcService.GetAllNestedItemBanksWithParentAsync(ItemBankRootId);

            Items.AddRange(fetchedItems);

            _treeViewKey--;
        }

        private void GetSelectedTreeItem(TreeItemResponseDto treeItem)
        {
            SelectedTreeItem = treeItem;
        }

        private void Submit()
        {
            var selectedTreeItemMapped = new SelectedItemBankNodeFromDialogDto
            {
                Id = SelectedTreeItem.Id,
                Name = SelectedTreeItem.Text,
                IsActive = SelectedTreeItem.IsActive
            };

            MudDialog.Close(DialogResult.Ok(selectedTreeItemMapped));
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
