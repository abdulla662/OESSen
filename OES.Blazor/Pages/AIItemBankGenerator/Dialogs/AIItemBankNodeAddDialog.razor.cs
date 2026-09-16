using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.ItemBankLevel;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.AIItemBankGenerator.Dialogs
{
    public partial class AIItemBankNodeAddDialog
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; } = default!;
        [Inject] private IBlazGroupService BlazGroupService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public AIItemBankNodeDto NodeModel { get; set; } = new();
        [Parameter] public bool IsLevelPredefined { get; set; }
        [Parameter] public ItemBankLevelsCreation ItemBankLevelsCreation { get; set; }
        [Parameter] public List<ItemLevelsDto> ExistingLevels { get; set; } = [];
        [Parameter] public List<AIItemBankNodeDto> ExistingSiblingNodes { get; set; } = [];

        private List<GetOESGroupDto> SelectedGroups { get; set; } = [];

        private MudForm _form = default!;
        private bool _isValid;
        private ItemBankLevelInsertionWay _insertionWay = ItemBankLevelInsertionWay.Selected;
        private ItemLevelsDto? _selectedExistingLevel;

        protected override void OnInitialized()
        {
            SelectedGroups = NodeModel?.OESGroupDtos?.ToList() ?? [];
        }

        private void OnExistingLevelSelected(ItemLevelsDto? level)
        {
            _selectedExistingLevel = level;
            NodeModel.Level = level?.Name ?? string.Empty;
            NodeModel.LevelId = level?.Id ?? 0;
        }

        private void OnInsertionWayChanged(ItemBankLevelInsertionWay way)
        {
            _insertionWay = way;
            NodeModel.Level = string.Empty;
            NodeModel.LevelId = 0;
            _selectedExistingLevel = null;
        }

        private async Task<IEnumerable<ItemLevelsDto>> SearchExistingLevelsAsync(string value, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            if (string.IsNullOrWhiteSpace(value))
            {
                return ExistingLevels;
            }

            return ExistingLevels.Where(l => l.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        private async Task OpenGroupDialog()
        {
            SelectedGroups = await DialogInteractionService.OpenSelectionDialogAsync(
                dialogService: DialogService,
                title: Resource.SelectGroup,
                preSelectedItems: SelectedGroups,
                endpointService: async pagination =>
                {
                    var allGroups = await BlazItemBankService.GetUserItemBankGroupsAsync();

                    if (!string.IsNullOrWhiteSpace(pagination.SearchKey))
                    {
                        allGroups = [.. allGroups.Where(g => g.Name.Contains(pagination.SearchKey, StringComparison.OrdinalIgnoreCase))];
                    }

                    return new CustomTableData<GetOESGroupDto>
                    {
                        Items = allGroups,
                        TotalItems = allGroups.Count
                    };
                },
                resourceType: ResourceType.ItemBank,
                onDelete: EventCallback.Factory.Create<Guid>(this, DeleteGroupAsync),
                isOpenedFromAI: true
            );
        }

        private async Task DeleteGroupAsync(Guid groupId)
        {
            var response = await BlazGroupService.DeleteGroupAsync(groupId);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
                Snackbar.Add(response.Message, Severity.Success);
            else
                Snackbar.Add(response.Message, Severity.Error);
        }

        private async Task Submit()
        {
            await _form.Validate();

            if (!_isValid)
                return;

            var nameExists = ExistingSiblingNodes.Any(x => string.Equals(x.Name?.Trim(), NodeModel.Name?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (nameExists)
            {
                Snackbar.Add(Resource.ItemBankNameAlreadyExists, Severity.Error);
                return;
            }

            var codeExists = ExistingSiblingNodes.Any(x => string.Equals(x.Code?.Trim(), NodeModel.Code?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (codeExists)
            {
                Snackbar.Add(Resource.ItemBankCodeAlreadyExists, Severity.Error);
                return;
            }

            NodeModel.OESGroupDtos = [.. SelectedGroups];

            MudDialog.Close(DialogResult.Ok(NodeModel));
        }

        private void Cancel()
        {
            MudDialog.Cancel();
        }
    }
}