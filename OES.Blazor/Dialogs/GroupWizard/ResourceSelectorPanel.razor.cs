using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Models.RuleMatrix;
using OES.Blazor.Services.Interfaces.GroupWizard;
using OES.Helper.Enums;

namespace OES.Blazor.Dialogs.GroupWizard
{
    public partial class ResourceSelectorPanel : ComponentBase
    {
        [Parameter] public ResourceType ResourceType { get; set; }
        [Parameter] public IGroupWizardResourceService ResourceService { get; set; } = default!;
        [Parameter] public List<long> SelectedIds { get; set; } = [];
        [Parameter] public EventCallback<List<long>> SelectedIdsChanged { get; set; }

        private bool _isLoading = true;
        private string _search = string.Empty;
        private HashSet<long> _selectedSet = [];

        // ── Flat (Questions, Papers, Schedule) ───────────────────────────────
        private MudTable<ResourceInstanceItem>? _table;

        // ── Tree (ItemBank, ILO) ─────────────────────────────────────────────
        private MudTable<ResourceInstanceItem>? _treeTable;

        // Cache current page roots to preserve expand state across ServerData calls
        private readonly List<ResourceInstanceItem> _currentPageRoots = [];

        protected override void OnInitialized()
        {
            _selectedSet = [.. SelectedIds];
            _isLoading = false;
        }

        // ── Tree ServerData ──────────────────────────────────────────────────

        private async Task<TableData<ResourceInstanceItem>> TreeServerReload(TableState state, CancellationToken _)
        {
            var result = await ResourceService.LoadRootsPageAsync(ResourceType, state.Page, state.PageSize, _search);

            var freshRoots = result.Items?.ToList() ?? [];

            // Preserve expand state from cache
            foreach (var root in freshRoots)
            {
                var cached = _currentPageRoots.FirstOrDefault(r => r.Id == root.Id);
                if (cached != null)
                {
                    root.IsExpanded = cached.IsExpanded;
                    root.Children = cached.Children;
                    root.ChildrenLoaded = cached.ChildrenLoaded;
                }
                root.IsSelected = _selectedSet.Contains(root.Id);
            }

            _currentPageRoots.Clear();
            _currentPageRoots.AddRange(freshRoots);

            // Restore child selections
            RestoreSelections(_currentPageRoots);

            // Build flat display list (roots + expanded children)
            var display = new List<ResourceInstanceItem>();
            BuildDisplayList(_currentPageRoots, display);

            return new TableData<ResourceInstanceItem>
            {
                TotalItems = result.TotalItems,
                Items = display
            };
        }

        private void OnTreeSearchChanged(string value)
        {
            _search = value;
            _currentPageRoots.Clear();
            _treeTable?.ReloadServerData();
        }

        private async Task ToggleExpand(ResourceInstanceItem item)
        {
            if (item.IsLeaf) return;

            if (!item.ChildrenLoaded)
            {
                item.IsLoadingChildren = true;
                StateHasChanged();
                item.Children = await ResourceService.LoadChildrenAsync(ResourceType, item.Id, item.Signature);
                foreach (var child in item.Children)
                {
                    child.IsSelected = _selectedSet.Contains(child.Id);
                    child.Depth = item.Depth + 1;
                }
                item.ChildrenLoaded = true;
                item.IsLeaf = item.Children.Count == 0;
                item.IsLoadingChildren = false;
            }

            item.IsExpanded = !item.IsExpanded;

            // Re-trigger ServerData to rebuild display list with/without children
            _treeTable?.ReloadServerData();
        }

        private void ToggleTreeItem(ResourceInstanceItem item)
        {
            if (_selectedSet.Contains(item.Id)) _selectedSet.Remove(item.Id);
            else _selectedSet.Add(item.Id);
            item.IsSelected = _selectedSet.Contains(item.Id);
            PropagateToChildren(item.Children, item.IsSelected);
            Notify();
            StateHasChanged();
        }

        private void SelectAllTree()
        {
            foreach (var item in _treeTable?.FilteredItems ?? [])
            { _selectedSet.Add(item.Id); item.IsSelected = true; }
            Notify();
            StateHasChanged();
        }

        private void ClearAllTree()
        {
            foreach (var item in _treeTable?.FilteredItems ?? [])
            { _selectedSet.Remove(item.Id); item.IsSelected = false; }
            Notify();
            StateHasChanged();
        }

        private static void BuildDisplayList(List<ResourceInstanceItem> nodes, List<ResourceInstanceItem> result)
        {
            foreach (var node in nodes)
            {
                result.Add(node);
                if (node.IsExpanded && node.ChildrenLoaded)
                    BuildDisplayList(node.Children, result);
            }
        }

        // ── Flat ServerData ──────────────────────────────────────────────────

        private async Task<TableData<ResourceInstanceItem>> ServerReload(TableState state, CancellationToken _)
        {
            var data = await ResourceService.FetchPageAsync(ResourceType, state, _search);

            if (data.Items != null)
                foreach (var item in data.Items)
                    item.IsSelected = _selectedSet.Contains(item.Id);

            return data;
        }

        private void OnSearchChanged(string value)
        {
            _search = value;
            _table?.ReloadServerData();
        }

        private void ToggleItem(ResourceInstanceItem item)
        {
            if (_selectedSet.Contains(item.Id)) _selectedSet.Remove(item.Id);
            else _selectedSet.Add(item.Id);
            item.IsSelected = _selectedSet.Contains(item.Id);
            Notify();
            StateHasChanged();
        }

        private void SelectAllOnPage()
        {
            foreach (var item in _table?.FilteredItems ?? [])
            { _selectedSet.Add(item.Id); item.IsSelected = true; }
            Notify();
            StateHasChanged();
        }

        private void ClearAllOnPage()
        {
            foreach (var item in _table?.FilteredItems ?? [])
            { _selectedSet.Remove(item.Id); item.IsSelected = false; }
            Notify();
            StateHasChanged();
        }

        private void Notify()
        {
            SelectedIds = [.. _selectedSet];
            _ = SelectedIdsChanged.InvokeAsync(SelectedIds);
        }

        private void RestoreSelections(List<ResourceInstanceItem> nodes)
        {
            foreach (var n in nodes)
            {
                n.IsSelected = _selectedSet.Contains(n.Id);
                RestoreSelections(n.Children);
            }
        }

        private static void PropagateToChildren(List<ResourceInstanceItem> nodes, bool value)
        {
            foreach (var n in nodes) { n.IsSelected = value; PropagateToChildren(n.Children, value); }
        }
    }
}