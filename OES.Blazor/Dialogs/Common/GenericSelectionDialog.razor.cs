using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;

namespace OES.Blazor.Dialogs.Common
{
    public partial class GenericSelectionDialog<TItem, TResult> : ComponentBase
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public string HeaderIcon { get; set; } = default!;
        [Parameter] public string HeaderTitle { get; set; } = default!;
        [Parameter] public string[] DisplayColumns { get; set; } = default!;
        [Parameter] public bool HideDescriptionSearch { get; set; }
        [Parameter] public bool IsSingleSelection { get; set; }
        [Parameter] public List<TItem> InitialSelectedItems { get; set; } = [];
        [Parameter] public ListItem<TItem>.EndPointDel EndpointService { get; set; } = default!;
        [Parameter] public Func<TItem, string> GetItemDisplayName { get; set; } = default!;
        [Parameter] public Func<List<TItem>, List<TResult>> ConvertToResult { get; set; } = default!;
        [Parameter] public List<TItem> LockedItems { get; set; } = [];
        [Parameter] public string LockedItemIcon { get; set; } = Icons.Material.Filled.Verified;

        private HashSet<TItem> SelectedItems = [];
        private int listChangingKey;
        private bool _isInitialized = false;

        protected override void OnParametersSet()
        {
            if (!_isInitialized)
            {
                if (InitialSelectedItems?.Count > 0)
                    SelectedItems = [.. InitialSelectedItems];

                SelectedItems.UnionWith(LockedItems);

                if (SelectedItems.Count > 0)
                    listChangingKey++;

                _isInitialized = true;
            }
        }

        private Task OnSelectedItemsChanged(HashSet<TItem> selectedItems)
        {
            if (IsSingleSelection && selectedItems.Count > 1)
            {
                var newItem = selectedItems.Except(SelectedItems).FirstOrDefault();

                selectedItems.Clear();

                if (!EqualityComparer<TItem>.Default.Equals(newItem, default))
                    selectedItems.Add(newItem);
            }

            SelectedItems = selectedItems;
            StateHasChanged();
            return Task.CompletedTask;
        }

        private void ConfirmSelection()
        {
            var allItems = LockedItems.Concat(SelectedItems.Where(item => !LockedItems.Contains(item))).ToList();
            var result = ConvertToResult(allItems);
            MudDialog.Close(DialogResult.Ok(result));
        }

        private void Cancel()
        {
            MudDialog.Cancel();
        }
    }
}
