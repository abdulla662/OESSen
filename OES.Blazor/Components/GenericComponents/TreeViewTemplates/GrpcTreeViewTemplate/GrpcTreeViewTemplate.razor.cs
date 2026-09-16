using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;

namespace OES.Blazor.Components.GenericComponents.TreeViewTemplates.GrpcTreeViewTemplate
{
    public partial class GrpcTreeViewTemplate : ComponentBase
    {
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter] public EventCallback<TreeItemResponseDto> InsertTreeItemInDb { get; set; } = default!;

        [Parameter] public EventCallback<TreeItemResponseDto> UpdateTreeItemInDb { get; set; } = default!;

        [Parameter] public EventCallback<TreeItemResponseDto> RemoveTreeItemFromDb { get; set; } = default!;

        [Parameter] public EventCallback<TreeItemResponseDto> ShowItemDetails { get; set; } = default!;

        [Parameter] public EventCallback<TreeItemResponseDto> GetSelectedTreeItem { get; set; } = default!;

        [Parameter] public EventCallback<List<TreeItemResponseDto>> GetSelectedTreeItems { get; set; } = default!;

        [Parameter] public bool InsertionButtonShown { get; set; } = true;

        [Parameter] public bool UpdateButtonShown { get; set; } = true;

        [Parameter] public bool DeletionButtonShown { get; set; } = true;

        [Parameter] public bool DetailsButtonShown { get; set; } = true;

        [Parameter] public bool IsSingleSelectionTree { get; set; } = false;

        [Parameter] public bool IsMultipleSelectionTree { get; set; } = false;

        [Parameter] public bool IsHierarchicalTree { get; set; } = false;

        [Parameter] public bool EnableCascadingSelection { get; set; } = false;

        [Parameter] public bool IsReadOnlyTree { get; set; } = false;

        [Parameter] public List<TreeItemResponseDto> OldSelectedItems { get; set; } = [];

        [Parameter] public bool OldSelectedItemsDisabled { get; set; } = false;

        [Parameter] public string LabelClass { get; set; }

        [Parameter] public string Label { get; set; }

        [Parameter] public long SelectedItemId { get; set; }

        [Parameter] public List<TreeItemResponseDto> Items { get; set; }

        [Parameter] public List<TreeItemResponseDto> SelectedItems { get; set; }

        [Parameter] public TreeCrudButtonsRolesPlaceholders TreeCrudButtonsRolesPlaceholders { get; set; }

        [Parameter] public RenderFragment<TreeItemResponseDto> NodeCustomButton { get; set; }

        [Parameter] public bool ShowSearchBar { get; set; } = false;


        private List<TreeItemResponseDto> BuiltTree { get; set; }


        private bool isLoading = false;

        private string searchText = string.Empty;

        private List<SearchResultItem> searchResults = [];


        protected override async Task OnInitializedAsync()
        {
            if (Items != null && Items.Count > 0)
            {
                BuiltTree = BuildTree(Items);
            }

            StateHasChanged();

            await Task.CompletedTask;
        }

        private static List<TreeItemResponseDto> BuildTree(List<TreeItemResponseDto> nodes)
        {
            var nodeDict = nodes.ToDictionary(n => n.Id);

            var rootNodes = new List<TreeItemResponseDto>();

            foreach (var node in nodes)
            {
                if (node.ParentId == null)
                {
                    rootNodes.Add(node);
                }
                else
                {
                    if (nodeDict.TryGetValue(Convert.ToInt64(node.ParentId), out var parent) && !parent.Children.Any(c => c.Id == node.Id))
                    {
                        parent.Children.Add(node);
                    }
                }
            }

            return rootNodes;
        }

        public async Task ExpandAll()
        {
            isLoading = true;

            await ExpandNodesInParallelAsync(Items);

            isLoading = false;
        }

        private async Task ExpandNodesInParallelAsync(List<TreeItemResponseDto> nodes)
        {
            var tasks = nodes.Select(async node =>
            {
                node.IsExpanded = true;

                StateHasChanged();

                await Task.Delay(1);

                if (node.Children.Count > 0)
                {
                    await ExpandNodesAsync(node.Children);
                }
            })
            .ToArray();

            await Task.WhenAll(tasks);
        }

        private async Task ExpandNodesAsync(List<TreeItemResponseDto> nodes)
        {
            foreach (var node in nodes)
            {
                if (!node.IsExpanded)
                {
                    node.IsExpanded = true;

                    StateHasChanged();

                    await Task.Delay(1);
                }

                if (node.Children.Count > 0)
                {
                    await ExpandNodesAsync(node.Children);
                }
            }
        }

        private void CollapseAll()
        {
            CollapseNodes(Items);

            StateHasChanged();
        }

        private static void CollapseNodes(List<TreeItemResponseDto> nodes)
        {
            foreach (var node in nodes)
            {
                node.IsExpanded = false;

                if (node.Children.Count > 0)
                {
                    CollapseNodes(node.Children);
                }
            }
        }

        private async Task SelectAll()
        {
            SelectedItems.Clear();
            SelectAllNodes(Items);
            await GetSelectedTreeItems.InvokeAsync(SelectedItems);
            StateHasChanged();
        }

        private void SelectAllNodes(List<TreeItemResponseDto> nodes)
        {
            foreach (var node in nodes)
            {
                if (!SelectedItems.Contains(node))
                {
                    SelectedItems.Add(node);
                }

                if (node.Children.Count > 0)
                {
                    SelectAllNodes(node.Children);
                }
            }
        }

        #region Search Methods

        private void OnSearchInput()
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                searchResults = [];
                return;
            }

            var itemDict = Items.ToDictionary(i => i.Id);

            searchResults = [.. Items
                .Where(item => item.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .Select(item => new SearchResultItem(item.Id, item.Text, BuildPath(item, itemDict)))
                .OrderBy(item => item.Text, StringComparer.OrdinalIgnoreCase)
            ];
        }

        private static string BuildPath(TreeItemResponseDto item, Dictionary<long, TreeItemResponseDto> itemDict)
        {
            var pathParts = new List<string>();

            var currentParentId = item.ParentId;

            while (currentParentId.HasValue && itemDict.TryGetValue(currentParentId.Value, out var parent))
            {
                pathParts.Insert(0, parent.Text);
                currentParentId = parent.ParentId;
            }

            return pathParts.Count > 0 ? string.Join(" › ", pathParts) : string.Empty;
        }

        private void ClearSearch()
        {
            searchText = string.Empty;

            searchResults = [];
        }

        private async Task NavigateToSearchResult(long itemId)
        {
            ClearSearch();

            ExpandPathToItem(itemId);

            StateHasChanged();

            await Task.Delay(1);

            await JSRuntime.InvokeVoidAsync(MiscConstants.ScrollToElement, itemId);
        }

        private void ExpandPathToItem(long targetId)
        {
            var itemDict = Items.ToDictionary(i => i.Id);

            if (!itemDict.TryGetValue(targetId, out var targetItem))
                return;

            var currentParentId = targetItem.ParentId;

            while (currentParentId.HasValue && itemDict.TryGetValue(currentParentId.Value, out var parent))
            {
                parent.IsExpanded = true;
                currentParentId = parent.ParentId;
            }
        }

        private (string Before, string Match, string After)? GetSearchParts(string text)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return null;

            var index = text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
                return null;

            return (
                text[..index],
                text.Substring(index, searchText.Length),
                text[(index + searchText.Length)..]
            );
        }

        private sealed record SearchResultItem(long Id, string Text, string Path);

        #endregion
    }
}
