using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.ItemBankLevels;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.ItemBank.API;
using OES.Helper.Dtos.ItemBankLevel;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Globalization;
using System.Text.Json;

namespace OES.Blazor.Pages.ItemBank.Dialog
{
    public partial class AddItemBankNodeDialog
    {
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; }
        [Inject] private IBlazGroupService BlazGroupService { get; set; }
        [Inject] private IBlazorItemBankLevelsService BlazItemBankLevelService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IDialogService DialogService { get; set; }


        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public ItemBankNode Model { get; set; } = new();

        [Parameter] public List<GetOESGroupDto> OesGroupsDtos { get; set; } = [];

        [Parameter] public long ParentLevelId { get; set; }

        [Parameter] public bool IsParentUnscored { get; set; }

        private List<GetOESGroupDto> SelectedGroups { get; set; } = [];
        private static bool RightToLeft => CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
        private ItemBankLevelInsertionWay ItemBankLevelInsertionWay { get; set; }
        private bool IsSubmitButtonDisabled =>
           string.IsNullOrWhiteSpace(Model.Name) ||
           string.IsNullOrWhiteSpace(Model.Code) ||
           string.IsNullOrWhiteSpace(Model.Level);

        private bool _unscored;

        private ItemBankLevelValidation _itemBankValidation = new();

        private bool _hoursValid = true;

        private List<ItemLevelsDto> _itemBankLevels = [];


        protected override async Task OnInitializedAsync()
        {
            try
            {
                _itemBankValidation = await BlazItemBankService.GetNodeValidation((long)Model.ParentId);

                Model.Level = _itemBankValidation.level;
                Model.Unscored = IsParentUnscored;

                _unscored = IsParentUnscored;

                List<ItemLevelsDto> allLevels = await BlazItemBankLevelService.GetLevels();

                _itemBankLevels = [.. allLevels
                    .Where(level => level.Id > ParentLevelId)
                    .OrderBy(l => l.Id)
                ];
            }
            finally
            {
                StateHasChanged();
            }
        }

        private void OnUnscoredChanged(bool value)
        {
            _unscored = value;
            Model.Unscored = value;
            StateHasChanged();
        }

        private async Task<IEnumerable<ItemLevelsDto>> SearchLevelsAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value))
                return _itemBankLevels;

            var filteredLevels = await Task.Run(() =>
                _itemBankLevels.Where(x => x.Name.Trim().ToLower().Contains(value.Trim().ToLower())),
                cancellationToken
            );

            return filteredLevels;
        }

        private void OnHoursChanged(float value)
        {
            Model.Hours = value;

            if (_itemBankValidation.ParentHours > 0)
            {
                _hoursValid = (value + _itemBankValidation.ChildHours) <= _itemBankValidation.ParentHours;
            }
            else
            {
                _hoursValid = true;
            }

            StateHasChanged();
        }

        private void AssignSelectedLevelToDtoModel(ItemLevelsDto itemLevelDto)
        {
            Model.LevelId = itemLevelDto?.Id ?? 0;
            Model.Level = itemLevelDto?.Name;
        }

        private void OnItemBankLevelInsertionWayChanged(ItemBankLevelInsertionWay itemBankLevelInsertionWay)
        {
            ItemBankLevelInsertionWay = itemBankLevelInsertionWay;

            Model.LevelId = 0;
            Model.Level = null;
        }

        private async Task OnValidSubmit()
        {
            if (string.IsNullOrWhiteSpace(Model.Level))
            {
                Snackbar.Add(Resource.MakeSureYouInsertedALevel, Severity.Error);
                return;
            }

            if (!_hoursValid)
            {
                Snackbar.Add(Resource.InvalidHours, Severity.Error);
                return;
            }

            Model.OESGroupDtos.Clear();
            Model.OESGroupDtos = [.. SelectedGroups];

            var response = await BlazItemBankService.AddNewNode(Model);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(response.Message, Severity.Success);

                var itemBankDto = JsonSerializer.Deserialize<NewItemBankDTO>(
                    response.Data.ToString(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                MudDialog.Close(DialogResult.Ok(itemBankDto));
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
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
                        allGroups = allGroups
                            .Where(g => g.Name.Contains(pagination.SearchKey, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    return new CustomTableData<GetOESGroupDto>
                    {
                        Items = allGroups,
                        TotalItems = allGroups.Count
                    };
                },
                resourceType: ResourceType.ItemBank,
                onDelete: EventCallback.Factory.Create<Guid>(this, DeleteGroupAsync)
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

        private void Cancel() => MudDialog.Cancel();
    }
}
