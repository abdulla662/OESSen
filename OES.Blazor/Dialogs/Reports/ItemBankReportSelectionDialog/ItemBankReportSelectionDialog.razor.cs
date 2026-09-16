using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TreeViewTemplates.GrpcTreeViewTemplate;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;

namespace OES.Blazor.Dialogs.Reports.ItemBankReportSelectionDialog
{
    public partial class ItemBankReportSelectionDialog
    {
        [Inject] private IBlazAnalyticalReportsService AnalyticalReportsService { get; set; } = default!;

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;

        private readonly List<TreeItemResponseDto> _treeItems = [];
        private TreeItemResponseDto? _selectedItem;
        private bool _isLoading = true;
        private int _treeKey;

        private static readonly TreeCrudButtonsRolesPlaceholders TreeRoles = new(SingleSelectionRole: OesTemplateRoleConstants.ItemBankViewer);

        protected override async Task OnInitializedAsync()
        {
            var itemBanks = await AnalyticalReportsService.GetItemBanksAsync();

            _treeItems.AddRange(BuildTree(itemBanks));

            _isLoading = false;

            _treeKey--;
        }

        private static List<TreeItemResponseDto> BuildTree(List<ItemBankLookupDto> banks)
        {
            var nodes = banks.ToDictionary(
                x => x.Id,
                x => new TreeItemResponseDto
                {
                    Id = x.Id,
                    Text = x.Name,
                    Code = x.Code ?? string.Empty,
                    ParentId = x.ParentId
                }
            );

            var roots = new List<TreeItemResponseDto>();

            foreach (var node in nodes.Values)
            {
                if (node.ParentId is null || !nodes.TryGetValue(node.ParentId.Value, out var parentNode))
                {
                    roots.Add(node);
                    continue;
                }

                parentNode.Children.Add(node);
            }

            return roots;
        }

        private void OnItemSelected(TreeItemResponseDto item) => _selectedItem = item;

        private void Submit()
        {
            if (_selectedItem is null)
            {
                return;
            }

            MudDialog.Close(DialogResult.Ok(new ItemBankLookupDto
            {
                Id = _selectedItem.Id,
                Name = _selectedItem.Text,
                Code = _selectedItem.Code
            }));
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
