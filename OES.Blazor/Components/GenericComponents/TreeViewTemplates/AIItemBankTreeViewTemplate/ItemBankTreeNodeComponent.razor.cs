using Microsoft.AspNetCore.Components;
using OES.Helper.Dtos.ItemBank;

namespace OES.Blazor.Components.GenericComponents.TreeViewTemplates.AIItemBankTreeViewTemplate
{
    public partial class ItemBankTreeNodeComponent : ComponentBase
    {

        [Parameter]
        public AIItemBankNodeDto Node { get; set; } = default!;

        [Parameter]
        public AIItemBankNodeDto RootNode { get; set; } = default!;

        [Parameter]
        public Dictionary<AIItemBankNodeDto, int> NodeNumbers { get; set; } = [];

        [Parameter]
        public EventCallback<AIItemBankNodeDto> InsertTreeItem { get; set; }

        [Parameter]
        public EventCallback<AIItemBankNodeDto> UpdateTreeItem { get; set; }

        [Parameter]
        public EventCallback<AIItemBankNodeDto> RemoveTreeItem { get; set; }

        [Parameter]
        public EventCallback<AIItemBankNodeDto> MoveTreeItem { get; set; }

        [Parameter]
        public bool InsertionButtonShown { get; set; }

        [Parameter]
        public bool UpdateButtonShown { get; set; }

        [Parameter]
        public bool DeletionButtonShown { get; set; }

        [Parameter]
        public bool MoveButtonShown { get; set; }

        [Parameter]
        public bool IsReadOnly { get; set; }

        [Parameter]
        public int Depth { get; set; } = 0;

        [Parameter]
        public AIItemBankNodeDto? SelectedNode { get; set; }

        [Parameter]
        public EventCallback<AIItemBankNodeDto> OnNodeSelected { get; set; }

        [Parameter]
        public string SearchText { get; set; } = string.Empty;

        protected bool ShowMenu { get; set; }

        protected bool IsSelected => ReferenceEquals(SelectedNode, Node);

        protected bool IsVisible()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            return IsNodeOrChildMatching(Node);
        }

        private bool IsNodeOrChildMatching(AIItemBankNodeDto node)
        {
            if (IsNodeMatching(node))
                return true;

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    if (IsNodeOrChildMatching(child))
                        return true;
                }
            }

            return false;
        }

        private bool IsNodeMatching(AIItemBankNodeDto node)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            return ContainsSearch(node.Name) ||
                   ContainsSearch(node.Code) ||
                   ContainsSearch(node.Level) ||
                   ContainsSearch(node.Description);
        }

        private bool ContainsSearch(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        protected async Task HandleNodeClick()
        {
            await OnNodeSelected.InvokeAsync(Node);
        }

        protected void ToggleNode()
        {
            Node.IsExpanded = !Node.IsExpanded;

            StateHasChanged();
        }

        protected void ToggleMenu()
        {
            ShowMenu = !ShowMenu;

            StateHasChanged();
        }

        protected async Task AddTreeItemAsync(AIItemBankNodeDto node)
        {
            await InsertTreeItem.InvokeAsync(node);

            StateHasChanged();
        }

        protected async Task UpdateTreeItemAsync(AIItemBankNodeDto node)
        {
            await UpdateTreeItem.InvokeAsync(node);

            StateHasChanged();
        }

        protected async Task RemoveTreeItemAsync(AIItemBankNodeDto node)
        {
            await RemoveTreeItem.InvokeAsync(node);

            StateHasChanged();
        }

        protected async Task MoveTreeItemAsync(AIItemBankNodeDto node)
        {
            await MoveTreeItem.InvokeAsync(node);

            StateHasChanged();
        }
    }
}
