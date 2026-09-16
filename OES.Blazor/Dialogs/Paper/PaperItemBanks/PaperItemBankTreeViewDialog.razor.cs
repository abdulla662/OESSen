using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TreeViewTemplates.GrpcTreeViewTemplate;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.GRPC;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;

namespace OES.Blazor.Dialogs.Paper.PaperItemBanks
{
    public partial class PaperItemBankTreeViewDialog : ComponentBase
    {
        [Inject] private IBlazItemBankService BlazorItemBankService { get; set; }

        [Inject] private IItemBankTreeGrpcService ItemBankTreeService { get; set; }

        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] private IBlazGroupService BlazGroupService { get; set; }

        [Inject] private IJSRuntime JSRuntime { get; set; }


        [CascadingParameter] public MudDialogInstance MudDialog { get; set; }

        [Parameter] public List<long> SelectedItemBankNodeIds { get; set; }

        [Parameter] public bool OldSelectedItemBankNodesDisabled { get; set; }


        private bool IsComponentInitialized { get; set; }

        private List<TreeItemResponseDto> Items { get; set; }

        private List<TreeItemResponseDto> FlattenedTreeItems { get; set; }

        private List<GetOESGroupDto> OesGroupsDtos { get; set; }

        private List<UserItemBankDto> UserItemBankDtos { get; set; } = [];

        private static TreeCrudButtonsRolesPlaceholders TreeCrudButtonsRolesPlaceholders =>
            new(MultipleSelectionRole: OesTemplateRoleConstants.PaperCreator);

        private List<TreeItemResponseDto> _currentSelectedItemBankNodes = [];
        private List<TreeItemResponseDto> _oldSelectedItemBankNodes = [];
        private GrpcTreeViewTemplate _treeViewRef;
        private bool _drawerOpen = false;

        protected override async Task OnInitializedAsync()
        {
            long storedIdOfParent = await BlazSessionStorageService.GetValue<long>("PerformViewBtnClick");

            Items = await ItemBankTreeService.GetAllNestedItemBanksWithParentAsync(storedIdOfParent) ?? [];

            FlattenedTreeItems = Items;

            IsComponentInitialized = true;

            UserItemBankDtos = await BlazorItemBankService.GetAllItemBankForUserAsync(storedIdOfParent) ?? [];

            OesGroupsDtos = await BlazGroupService.GetGroupsAsync() ?? [];

            _currentSelectedItemBankNodes = [.. Items.Where(x => SelectedItemBankNodeIds.Contains(x.Id))];

            _oldSelectedItemBankNodes = [.. Items.Where(x => SelectedItemBankNodeIds.Contains(x.Id))];

            StateHasChanged();
        }

        private void ToggleDrawer()
        {
            _drawerOpen = !_drawerOpen;
        }

        private async Task ScrollToItem(long itemId)
        {
            await _treeViewRef.ExpandAll();

            await JSRuntime.InvokeVoidAsync("scrollToElement", itemId);

            ToggleDrawer();
        }

        private void GetSelectedTreeItems(List<TreeItemResponseDto> treeItems)
        {
            _currentSelectedItemBankNodes = treeItems;
        }

        private void Submit()
        {
            MudDialog.Close(DialogResult.Ok(_currentSelectedItemBankNodes));
        }

        private void Cancel()
        {
            MudDialog.Cancel();
        }
    }
}
