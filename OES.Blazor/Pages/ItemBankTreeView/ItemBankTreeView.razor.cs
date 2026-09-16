using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Components.GenericComponents.TreeViewTemplates.GrpcTreeViewTemplate;
using OES.Blazor.Pages.ItemBank;
using OES.Blazor.Pages.ItemBank.Dialog;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.GRPC;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.ItemBank.API;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.ItemBankTreeView
{
    public partial class ItemBankTreeView : ComponentBase
    {
        [Inject] private IBlazItemBankService BlazorItemBankService { get; set; }

        [Inject] private IDialogService DialogService { get; set; } = default!;

        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Inject] private IItemBankTreeGrpcService ItemBankTreeService { get; set; }

        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] private IBlazGroupService BlazGroupService { get; set; }

        [Inject] private IBlazAuthService AuthService { get; set; } = default!;

        [Inject] private IJSRuntime JSRuntime { get; set; }


        [Parameter] public ItemBankNode Model { get; set; } = new();

        [Parameter] public EventCallback<TreeItemResponseDto> SetUnscoredForChildren { get; set; }

        private ItemBankLevelValidation ItemBankValidation { get; set; } = new();

        private string DeletedItemBankName { get; set; } = string.Empty;

        private bool IsComponentInitialized { get; set; }

        private List<TreeItemResponseDto> Items { get; set; } = [];

        private TreeItemResponseDto CurrentItem { get; set; }

        private List<TreeItemResponseDto> FlattenedTreeItems { get; set; } = [];

        private List<GetOESGroupDto> OesGroupsDtos { get; set; } = [];

        private List<UserItemBankDto> UserItemBankDtos { get; set; } = [];

        private static bool RightToLeft => System.Globalization.CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;

        private static TreeCrudButtonsRolesPlaceholders TreeCrudButtonsRolesPlaceholders =>
            new(InsertionRole: OesTemplateRoleConstants.ItemBankCreator,
                UpdateRole: OesTemplateRoleConstants.ItemBankEditor,
                DeletionRole: OesTemplateRoleConstants.ItemBankDeleter,
                ViewRole: OesTemplateRoleConstants.ItemBankViewer
            );


        private GrpcTreeViewTemplate? _treeViewRef;

        private bool _drawerOpen = false;

        private long _storedIdOfParent = 0;

        private int _treeKey = 0;

        protected override async Task OnInitializedAsync()
        {
            _storedIdOfParent = await BlazSessionStorageService.GetValue<long>("PerformViewBtnClick");
            await LoadTreeDataAsync();
            IsComponentInitialized = true;
            UserItemBankDtos = await BlazorItemBankService.GetAllItemBankForUserAsync(_storedIdOfParent);
            OesGroupsDtos = await BlazGroupService.GetGroupsAsync();
        }

        private async Task LoadTreeDataAsync()
        {
            var freshData = await ItemBankTreeService.GetAllNestedItemBanksWithParentAsync(_storedIdOfParent);

            Items = [];
            FlattenedTreeItems = [];

            await InvokeAsync(StateHasChanged);

            Items = freshData;
            FlattenedTreeItems = freshData;
            _treeKey++;

            await InvokeAsync(StateHasChanged);
        }

        public async Task InsertItemInDbAsync(TreeItemResponseDto parentTreeItem)
        {
            if (!parentTreeItem.IsActive)
            {
                Snackbar.Add(Resource.CannotAddNodeBecauseParentIsInactive, Severity.Error);
                return;
            }

            ItemBankValidation = await BlazorItemBankService.GetNodeValidation(parentTreeItem.Id);
            var freshParentData = await BlazorItemBankService.GetEditItemBank(parentTreeItem.Id);
            var parentUnscored = freshParentData?.Unscored ?? parentTreeItem.Unscored;

            var parameters = new DialogParameters<AddItemBankNodeDialog>
            {
                {
                    dialog => dialog.Model,
                    new ItemBankNode
                    {
                        Name = "",
                        Code = "",
                        Description = "",
                        Level = ItemBankValidation.level,
                        ParentId = parentTreeItem.Id,
                        //Hours = 0,
                        LevelId = 0,
                    }
                },
                {
                    dialog => dialog.OesGroupsDtos, OesGroupsDtos
                },
                {
                    dialog => dialog.ParentLevelId, parentTreeItem.LevelId
                },
                {
                    dialog => dialog.IsParentUnscored, parentUnscored
                }
            };

            DialogOptions options = new()
            {
                CloseOnEscapeKey = true,
                BackdropClick = false,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium
            };

            var result = await DialogService.Show<AddItemBankNodeDialog>(Resource.AddNewNode, parameters, options).Result;

            if (!result.Canceled && result.Data is NewItemBankDTO itemBankDto)
            {
                var fullNodeDto = await BlazorItemBankService.GetEditItemBank(itemBankDto.Id);

                var newInsertedItemBankDto = new TreeItemResponseDto
                {
                    Id = itemBankDto.Id,
                    Text = fullNodeDto.Name,
                    Code = fullNodeDto.Code,
                    Description = fullNodeDto.Description,
                    //Hours = fullNodeDto.Hours,
                    ParentId = fullNodeDto.ParentId,
                    IsChildrenLoaded = true,
                    Children = [],
                    IsExpanded = false,
                    Signature = itemBankDto.OrganizationSignature,
                    IsActive = itemBankDto.IsActive,
                    GroupsIds = fullNodeDto.OESGroupDtos?.Select(x => x.Id).ToList() ?? [],
                    LevelId = (long)itemBankDto.LevelId
                };

                parentTreeItem.Children.Add(newInsertedItemBankDto);
                FlattenedTreeItems.Add(newInsertedItemBankDto);

                StateHasChanged();
            }
        }

        public async Task UpdateItemInDbAsync(TreeItemResponseDto selectedItemBankDto)
        {
            var parents = await ItemBankTreeService.GetAllowedParentsOfItemBankNodeAsync(selectedItemBankDto.ParentId);

            if (parents == null || parents.Count == 0)
            {
                Snackbar.Add(Resource.Failedtofetchparentitems, Severity.Error);

                return;
            }

            var freshData = await BlazorItemBankService.GetEditItemBank(selectedItemBankDto.Id);

            var parentNode = FlattenedTreeItems.Find(x => x.Id == selectedItemBankDto.ParentId);

            bool parentIsActive = parentNode?.IsActive ?? true;

            var parameters = new DialogParameters<ItemBankEditDialog>
            {
                { x => x.EditItemBankNodeDto, new EditItemBankNodeDto
                    {
                        Id = selectedItemBankDto.Id,
                        Name = selectedItemBankDto.Text,
                        Description = selectedItemBankDto.Description,
                        Unscored = freshData.Unscored,
                        Code = selectedItemBankDto.Code,
                        //Hours = selectedItemBankDto.Hours,
                        ParentId = selectedItemBankDto.ParentId,
                        IsActive = selectedItemBankDto.IsActive,
                        ParentIsActive = parentIsActive,
                        OrganizationId = selectedItemBankDto.OrganizationId,
                        Signature = selectedItemBankDto.Signature,
                        OESGroupDtos = [.. OesGroupsDtos.IntersectBy(selectedItemBankDto.GroupsIds, static x => x.Id)]
                    }
                },
                {
                    dialog => dialog.OesGroupsDtos, OesGroupsDtos
                },
                { x => x.Parents, parents }
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                BackdropClick = false,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium
            };

            var dialog = await DialogService.ShowAsync<ItemBankEditDialog>(Resource.EditItemBank, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is EditItemBankNodeDto updatedItemBank)
            {
                if (selectedItemBankDto.ParentId != updatedItemBank.ParentId)
                {
                    var oldParentItem = FlattenedTreeItems.Find(item => item.Id == selectedItemBankDto.ParentId);
                    oldParentItem?.Children.Remove(selectedItemBankDto);
                    FlattenedTreeItems.Remove(selectedItemBankDto);

                    var newParentItem = FlattenedTreeItems.Find(item => item.Id == updatedItemBank.ParentId);
                    if (newParentItem != null)
                    {
                        var movedChild = new TreeItemResponseDto
                        {
                            Id = selectedItemBankDto.Id,
                            Text = updatedItemBank.Name,
                            Description = updatedItemBank.Description,
                            Code = updatedItemBank.Code,
                            //Hours = updatedItemBank.Hours,
                            ParentId = updatedItemBank.ParentId,
                            IsActive = updatedItemBank.IsActive,
                            Children = selectedItemBankDto.Children,
                            GroupsIds = updatedItemBank.OESGroupDtos.ConvertAll(x => x.Id),
                            Signature = selectedItemBankDto.Signature,
                            OrganizationId = selectedItemBankDto.OrganizationId,
                            LevelId = updatedItemBank.LevelId,
                            Unscored = updatedItemBank.Unscored
                        };

                        newParentItem.Children.Add(movedChild);
                        FlattenedTreeItems.Add(movedChild);

                        CascadeIsActiveToDescendants(movedChild, updatedItemBank.IsActive);
                    }
                }
                else
                {
                    selectedItemBankDto.Text = updatedItemBank.Name;
                    selectedItemBankDto.Description = updatedItemBank.Description;
                    selectedItemBankDto.Code = updatedItemBank.Code;
                    selectedItemBankDto.IsActive = updatedItemBank.IsActive;
                    //selectedItemBankDto.Hours = updatedItemBank.Hours;
                    selectedItemBankDto.ParentId = updatedItemBank.ParentId;
                    selectedItemBankDto.GroupsIds = updatedItemBank.OESGroupDtos.ConvertAll(x => x.Id);
                    selectedItemBankDto.Unscored = updatedItemBank.Unscored;
                    CascadeIsActiveToDescendants(selectedItemBankDto, updatedItemBank.IsActive);
                }

                var itemInTree = Items.FirstOrDefault(x => x.Id == updatedItemBank.Id);

                if (itemInTree != null)
                {
                    itemInTree.Text = updatedItemBank.Name;
                    itemInTree.Description = updatedItemBank.Description;
                    itemInTree.IsActive = updatedItemBank.IsActive;
                    //itemInTree.Hours = updatedItemBank.Hours;
                    itemInTree.GroupsIds = updatedItemBank.OESGroupDtos.ConvertAll(g => g.Id);
                    itemInTree.ParentId = updatedItemBank.ParentId;
                    itemInTree.Unscored = updatedItemBank.Unscored;
                }

                await LoadTreeDataAsync();

                StateHasChanged();
            }
        }

        public async Task RemoveItemFromDbAsync(TreeItemResponseDto treeItem)
        {
            CurrentItem = treeItem;

            var response = await BlazorItemBankService.CanSoftDeleteItemBankAsync(treeItem.Id);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                DeletedItemBankName = treeItem.Text;

                var parameters = new DialogParameters
                {
                    { nameof(GenericDialog.Title), Resource.AreYouSure },
                    { nameof(GenericDialog.Content), $"{Resource.Doyouwanttodelete} {DeletedItemBankName} {Resource.QuestionMark}"},
                    { nameof(GenericDialog.SubmitText), Resource.Delete },
                    { nameof(GenericDialog.SubmitButtonColor), Color.Error },
                    { nameof(GenericDialog.SubmitButtonStartIcon), Icons.Material.Filled.Delete },
                    { nameof(GenericDialog.ShowAdditionalButton), treeItem.Children.Count > 0 },
                    { nameof(GenericDialog.AdditionalButtonText), Resource.DeleteThenTransferChilds },
                    { nameof(GenericDialog.AdditionalButtonStartIcon), Icons.Material.Filled.DeleteSweep },
                    { nameof(GenericDialog.AdditionalButtonColor), Color.Primary },
                    { nameof(GenericDialog.CancelText), Resource.Cancel },
                    { nameof(GenericDialog.CancelButtonStartIcon), Icons.Material.Filled.Close },
                    { nameof(GenericDialog.AdditionalButtonCallBack), EventCallback.Factory.Create(this, () => TransferChildrenForNodeAsync(treeItem)) },
                };

                var options = new DialogOptions
                {
                    CloseOnEscapeKey = true,
                    FullWidth = true,
                    MaxWidth = MaxWidth.Small,
                };

                var dialog = await DialogService.ShowAsync<GenericDialog>(
                    Resource.ConfirmDelete,
                    parameters,
                    options
                );

                var result = await dialog.Result;

                if (!result.Canceled && result.Data is true)
                {
                    await BlazorItemBankService.ExecuteSoftDeleteForNodeAsync(treeItem.Id);

                    var parentItem = FlattenedTreeItems.Find(item => item.Id == treeItem.ParentId);

                    parentItem.Children.Remove(treeItem);
                    FlattenedTreeItems.Remove(treeItem);

                    Snackbar.Add(Resource.Nodeahavebeendeletedsuccessfully, Severity.Success);

                    StateHasChanged();
                }
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        public async Task TransferChildrenForNodeAsync(TreeItemResponseDto treeItem)
        {
            await BlazorItemBankService.TransferChildrenForNodeAsync(treeItem.Id);

            var parentItem = FlattenedTreeItems.Find(item => item.Id == treeItem.ParentId);

            var children = new List<TreeItemResponseDto>(treeItem.Children);

            parentItem.Children.Remove(treeItem);
            FlattenedTreeItems.Remove(treeItem);

            foreach (var child in children)
            {
                child.ParentId = parentItem.Id;

                parentItem.Children.Add(child);
                FlattenedTreeItems.Add(child);
            }

            Snackbar.Add(Resource.Nodedeletedanditschildrenhavebeentransferredtoahighernodesuccessfully, Severity.Success);

            StateHasChanged();
        }

        public async Task ShowItemDetailsAsync(TreeItemResponseDto treeItem)
        {
            var parameters = new DialogParameters<ItemBankDetailsDialogBox.ItemBankDetailsDialogBox>
            {
                {
                    dialog => dialog.ItemBankDetailsObject, treeItem
                },
                {
                    dialog => dialog.OesGroupsDtos, OesGroupsDtos
                }
            };

            DialogOptions options = new()
            {
                CloseOnEscapeKey = true,
                BackdropClick = false,
                FullWidth = true,
                CloseButton = true,
                MaxWidth = MaxWidth.Small
            };

            await DialogService.ShowAsync<ItemBankDetailsDialogBox.ItemBankDetailsDialogBox>(Resource.ItemBankDetails, parameters, options);
        }

        public async Task TransferItemBankQuestionsAsync(TreeItemResponseDto treeItem)
        {
            var userAuthorized = await AuthService.IsCurrentUserAuthorizedAsync(
                treeItem.Id,
                OesTemplateRoleConstants.ItemBankQuestionReplacer,
                BlazorItemBankService.GetItemBankGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!userAuthorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            CurrentItem = treeItem;

            var parameters = new DialogParameters<TransferQuestionsToItemBankDialog>
            {
                { p => p.ItemBankRootId, _storedIdOfParent },
                { p => p.SelectedSourceItemBankNodeId, treeItem.Id },
                { p=> p.SourceItemBankUnscored, treeItem.Unscored }
            };

            var options = new DialogOptions()
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            await DialogService.ShowAsync<TransferQuestionsToItemBankDialog>(string.Empty, parameters, options);
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

        private static void CascadeIsActiveToDescendants(TreeItemResponseDto node, bool isActive)
        {
            foreach (var child in node.Children)
            {
                child.IsActive = isActive;
                CascadeIsActiveToDescendants(child, isActive);
            }
        }
    }
}