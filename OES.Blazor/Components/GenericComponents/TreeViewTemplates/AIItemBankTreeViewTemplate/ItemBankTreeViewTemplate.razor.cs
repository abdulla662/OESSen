using Microsoft.AspNetCore.Components;
using OES.Helper.Dtos.ItemBank;

namespace OES.Blazor.Components.GenericComponents.TreeViewTemplates.AIItemBankTreeViewTemplate
{
    public partial class ItemBankTreeViewTemplate : ComponentBase
    {
        [Parameter]
        public AIItemBankNodeDto? RootNode { get; set; }

        [Parameter]
        public EventCallback<AIItemBankNodeDto> InsertTreeItem { get; set; }

        [Parameter]
        public EventCallback<AIItemBankNodeDto> UpdateTreeItem { get; set; }

        [Parameter]
        public EventCallback<AIItemBankNodeDto> RemoveTreeItem { get; set; }

        [Parameter]
        public EventCallback<AIItemBankNodeDto> MoveTreeItem { get; set; }

        [Parameter]
        public bool InsertionButtonShown { get; set; } = true;

        [Parameter]
        public bool UpdateButtonShown { get; set; } = true;

        [Parameter]
        public bool DeletionButtonShown { get; set; } = true;

        [Parameter]
        public bool MoveButtonShown { get; set; } = true;

        [Parameter]
        public bool IsReadOnly { get; set; }

        [Parameter]
        public bool ShowSearchBar { get; set; } = true;

        [Parameter]
        public AIItemBankNodeDto? SelectedNode { get; set; }

        [Parameter]
        public EventCallback<AIItemBankNodeDto?> SelectedNodeChanged { get; set; }

        protected bool IsLoading { get; set; }

        protected string SearchText { get; set; } = string.Empty;

        protected Dictionary<AIItemBankNodeDto, int> NodeNumbers { get; set; } = [];


        protected override void OnParametersSet()
        {
            AssignNodeNumbers();

            if (RootNode != null && !string.IsNullOrWhiteSpace(SearchText))
            {
                ApplySearch(RootNode);
            }

            base.OnParametersSet();
        }

        protected void OnSearchTextChanged(string text)
        {
            SearchText = text;

            if (RootNode != null)
            {
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    ApplySearch(RootNode);
                }
            }

            StateHasChanged();
        }

        protected async Task OnNodeSelected(AIItemBankNodeDto node)
        {
            SelectedNode = node;
            await SelectedNodeChanged.InvokeAsync(SelectedNode);
            StateHasChanged();
        }

        private void AssignNodeNumbers()
        {
            NodeNumbers.Clear();

            if (RootNode == null)
                return;

            var currentNumber = 0;

            AssignNodeNumbersRecursive(
                RootNode,
                ref currentNumber
            );
        }

        private void AssignNodeNumbersRecursive(
            AIItemBankNodeDto node,
            ref int currentNumber
        )
        {
            NodeNumbers[node] = currentNumber++;

            if (node.Children == null || node.Children.Count == 0)
            {
                return;
            }

            foreach (var child in node.Children)
            {
                AssignNodeNumbersRecursive(
                    child,
                    ref currentNumber);
            }
        }

        protected async Task ExpandAllAsync()
        {
            if (RootNode == null)
                return;

            IsLoading = true;

            await ExpandNodeRecursiveAsync(RootNode);

            IsLoading = false;

            StateHasChanged();
        }

        private async Task ExpandNodeRecursiveAsync(AIItemBankNodeDto node)
        {
            node.IsExpanded = true;

            StateHasChanged();

            await Task.Delay(1);

            if (node.Children == null)
                return;

            foreach (var child in node.Children)
            {
                await ExpandNodeRecursiveAsync(child);
            }
        }

        protected void CollapseAll()
        {
            if (RootNode == null)
                return;

            CollapseNodeRecursive(RootNode);

            StateHasChanged();
        }

        private static void CollapseNodeRecursive(AIItemBankNodeDto node)
        {
            node.IsExpanded = false;

            if (node.Children == null)
                return;

            foreach (var child in node.Children)
            {
                CollapseNodeRecursive(child);
            }
        }

        private bool ApplySearch(AIItemBankNodeDto node)
        {
            var nodeMatched = IsNodeMatchingSearch(node);

            var childMatched = false;

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    if (ApplySearch(child))
                    {
                        childMatched = true;
                    }
                }
            }

            if (childMatched)
            {
                node.IsExpanded = true;
            }

            return nodeMatched || childMatched;
        }

        private bool IsNodeMatchingSearch(AIItemBankNodeDto node)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            return
                ContainsSearch(node.Name) ||
                ContainsSearch(node.Code) ||
                ContainsSearch(node.Level) ||
                ContainsSearch(node.Description);
        }

        private bool ContainsSearch(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }
    }
}