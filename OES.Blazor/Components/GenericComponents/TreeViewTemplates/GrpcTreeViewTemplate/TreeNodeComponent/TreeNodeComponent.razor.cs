using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using SharedHelper.RolesNames;

namespace OES.Blazor.Components.GenericComponents.TreeViewTemplates.GrpcTreeViewTemplate.TreeNodeComponent
{
    public partial class TreeNodeComponent : ComponentBase
    {
        // Component's Injections:

        [Inject] private GlobalUserContext GlobalUserContext { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; }


        // Component's Parameters and Properties:

        [Parameter] public EventCallback<TreeItemResponseDto> InsertTreeItemInDb { get; set; }

        [Parameter] public EventCallback<TreeItemResponseDto> UpdateTreeItemInDb { get; set; }

        [Parameter] public EventCallback<TreeItemResponseDto> RemoveTreeItemFromDb { get; set; }

        [Parameter] public EventCallback<TreeItemResponseDto> ShowItemDetails { get; set; }

        [Parameter] public EventCallback<TreeItemResponseDto> GetSelectedTreeItem { get; set; }

        [Parameter] public EventCallback<List<TreeItemResponseDto>> GetSelectedTreeItems { get; set; }

        [Parameter] public bool InsertionButtonShown { get; set; }

        [Parameter] public bool UpdateButtonShown { get; set; }

        [Parameter] public bool DeletionButtonShown { get; set; }

        [Parameter] public bool DetailsButtonShown { get; set; }

        [Parameter] public bool IsSingleSelectionTree { get; set; } = false;

        [Parameter] public bool IsMultipleSelectionTree { get; set; } = false;

        [Parameter] public bool IsHierarchicalTree { get; set; } = false;

        [Parameter] public bool EnableCascadingSelection { get; set; } = false;

        [Parameter] public bool IsReadOnlyTree { get; set; } = false;

        [Parameter] public List<TreeItemResponseDto> OldSelectedItems { get; set; } = [];

        [Parameter] public bool OldSelectedItemsDisabled { get; set; } = false;

        [Parameter] public long SelectedItemId { get; set; }

        [Parameter] public List<TreeItemResponseDto> SelectedItems { get; set; } = [];

        [Parameter] public TreeItemResponseDto Node { get; set; }

        [Parameter] public TreeCrudButtonsRolesPlaceholders TreeCrudButtonsRolesPlaceholders { get; set; }

        [Parameter] public RenderFragment<TreeItemResponseDto> NodeCustomButton { get; set; }

        private IEnumerable<string> CurrentUserGroupsMixedRoles { get; set; } = [];

        private bool IsCurrentUserSuperAdminOrEntityAdmin =>
            GlobalUserContext.SsoUserRoles.Exists(x => x.Name == AdminRoles.SuperAdmin || x.Name == AdminRoles.Entity_Admin);

        private string InsertionButtonTooltip => IsHierarchicalTree && Node?.Children?.Count > 0 ?
            Resource.CannotAddSiblingToSameParentToolTip :
            Resource.AddNewTreeItem;

        private bool ShowMenu { get; set; }


        // Life-cycle Methods:

        protected override void OnInitialized()
        {
            FindIntersectedRoles();

            StateHasChanged();
        }


        // Methods for Toggling Node and Toggling Menu

        private void ToggleNode()
        {
            Node.IsExpanded = !Node.IsExpanded;

            StateHasChanged();
        }

        private void ToggleMenu()
        {
            if (!ShowMenu)
            {
                FindIntersectedRoles();
            }

            ShowMenu = !ShowMenu;

            StateHasChanged();
        }


        // Operations Methods:

        private async Task AddNewTreeItemAsync(TreeItemResponseDto treeItem)
        {
            treeItem.GroupsIds ??= [];
            treeItem.Children ??= [];

            await InsertTreeItemInDb.InvokeAsync(treeItem);

            StateHasChanged();
        }

        private async Task UpdateTreeItemAsync(TreeItemResponseDto treeItem)
        {
            await UpdateTreeItemInDb.InvokeAsync(treeItem);

            StateHasChanged();
        }

        private async Task RemoveTreeItemAsync(TreeItemResponseDto treeItem)
        {
            await RemoveTreeItemFromDb.InvokeAsync(treeItem);

            StateHasChanged();
        }

        private async Task ShowTreeItemDetailsAsync(TreeItemResponseDto treeItem)
        {
            await ShowItemDetails.InvokeAsync(treeItem);
            StateHasChanged();
        }

        private async Task FetchSelectedTreeItem(TreeItemResponseDto treeItem)
        {
            await GetSelectedTreeItem.InvokeAsync(treeItem);
        }

        private async Task FetchSelectedTreeItems(TreeItemResponseDto treeItem)
        {
            if (EnableCascadingSelection)
            {
                bool isCurrentlySelected = SelectedItems?.Select(static x => x.Id).Contains(treeItem.Id) == true;

                if (isCurrentlySelected)
                {
                    RemoveItemAndChildren(treeItem);
                }
                else
                {
                    AddItemAndChildren(treeItem);
                }
            }
            else
            {
                // Normal selection: select/deselect only the clicked item
                if (SelectedItems.Select(static x => x.Id).Contains(treeItem.Id))
                {
                    SelectedItems.RemoveAll(x => x.Id == treeItem.Id);
                }
                else
                {
                    SelectedItems.Add(treeItem);
                }
            }

            await GetSelectedTreeItems.InvokeAsync(SelectedItems);
        }

        private void AddItemAndChildren(TreeItemResponseDto item)
        {
            // Add to SelectedItems if it's being used
            if (SelectedItems != null && !SelectedItems.Select(x => x.Id).Contains(item.Id))
            {
                SelectedItems.Add(item);
            }

            // Recursively add children
            if (item.Children?.Count > 0)
            {
                foreach (var child in item.Children)
                {
                    AddItemAndChildren(child);
                }
            }
        }

        private void RemoveItemAndChildren(TreeItemResponseDto item)
        {
            SelectedItems?.RemoveAll(x => x.Id == item.Id);

            if (item.Children?.Count > 0)
            {
                foreach (var child in item.Children)
                {
                    RemoveItemAndChildren(child);
                }
            }
        }


        // Helper Methods:

        private void FindIntersectedRoles()
        {
            Node.GroupsIds ??= [];
            Node.Children ??= [];

            CurrentUserGroupsMixedRoles = GlobalUserContext
                .OesUserGroupsAndRoles
                .Where(group => Node.GroupsIds.Contains(group.GroupId))
                .SelectMany(group => group.GroupRoles.Select(role => role.Name));
        }

        private bool HasAccess(string role)
        {
            return IsCurrentUserSuperAdminOrEntityAdmin ||
                   TreeCrudButtonsRolesPlaceholders is null ||
                   string.IsNullOrWhiteSpace(role) ||
                   CurrentUserGroupsMixedRoles.Contains(TreeCrudButtonsRolesPlaceholders?.FullAccessRole) ||
                   CurrentUserGroupsMixedRoles.Contains(role);
        }
    }
}
