using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.GRPC;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.ItemBank.Dialog
{
    public partial class TransferQuestionsToItemBankDialog
    {
        [Inject] private IItemBankTreeGrpcService ItemBankTreeGrpcService { get; set; }
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }
        [Parameter] public long ItemBankRootId { get; set; }
        [Parameter] public long SelectedSourceItemBankNodeId { get; set; }
        [Parameter] public bool SourceItemBankUnscored { get; set; }

        private List<TreeItemResponseDto> Items { get; set; } = [];


        private TreeItemResponseDto _selectedItemBankNode = new();

        private int _treeViewKey;

        private bool _waitingResponse;


        protected override async Task OnInitializedAsync()
        {
            var fetchedItems = await ItemBankTreeGrpcService.GetAllNestedItemBanksWithParentAsync(ItemBankRootId);

            Items.AddRange(fetchedItems.Where(x => x.Id != SelectedSourceItemBankNodeId));

            _treeViewKey--;
        }

        private void GetSelectedTreeItem(TreeItemResponseDto treeItem)
        {
            _selectedItemBankNode = treeItem;
        }

        private async Task SubmitAsync(ItemBankQuestionsTransferType transferType)
        {
            _waitingResponse = true;

            if (SourceItemBankUnscored != _selectedItemBankNode.Unscored)
            {
                var warningMessage = _selectedItemBankNode.Unscored
                    ? Resource.FutureItemBankUnscoredWarning
                    : Resource.FutureItemBankScoredWarning;

                var confirmed = await DialogService.ShowMessageBox(
                    Resource.Waiting,
                    warningMessage,
                    yesText: Resource.Confirm,
                    cancelText: Resource.Close
                );

                if (confirmed != true)
                {
                    _waitingResponse = false;
                    return;
                }
            }

            var transferDto = new TransferQuestionsToItemBankDto(SelectedSourceItemBankNodeId, _selectedItemBankNode.Id, transferType);

            var response = await BlazItemBankService.TransferQuestionsToItemBankAsync(transferDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);
                MudDialog.Close();
            }
            else if (response.StatusCode == HttpStatusCode.NotModified)
            {
                Snackbar.Add(response.Message, Severity.Info);
                MudDialog.Close();
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            _waitingResponse = false;
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
