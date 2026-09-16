using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.GRPC;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.TreeItem;

namespace OES.Blazor.Dialogs.Question.IloSelectionDialog
{
    public partial class IloSelectionDialog
    {
        [Inject] private IILoTreeGrpcService ILoTreeGrpcService { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

        [Parameter] public long IloRootId { get; set; }

        [Parameter] public long SelectedIloNodeId { get; set; }

        private List<TreeItemResponseDto> Items { get; set; } = [];

        private TreeItemResponseDto? SelectedTreeItem { get; set; }


        private int _treeViewKey;


        protected override async Task OnInitializedAsync()
        {
            var fetchedItems = await ILoTreeGrpcService.GetAllNestedIlosWithParentAsync(IloRootId);

            Items.AddRange(fetchedItems);

            _treeViewKey--;
        }

        private void GetSelectedTreeItem(TreeItemResponseDto treeItem)
        {
            SelectedTreeItem = treeItem;
        }

        private void Submit()
        {
            var selectedTreeItemMapped = new SelectedIloNodeFromDialogDto
            {
                Id = SelectedTreeItem.Id,
                Name = SelectedTreeItem.Text,
            };

            MudDialog.Close(DialogResult.Ok(selectedTreeItemMapped));
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
