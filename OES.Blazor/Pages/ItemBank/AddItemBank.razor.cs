using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.ItemBankLevels;
using OES.Helper.Dtos.ItemBank.API;
using OES.Helper.Dtos.ItemBankLevel;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.ItemBank
{
    public partial class AddItemBank : ComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazItemBankService BlazItemBankService { get; set; }
        [Inject] IBlazorItemBankLevelsService BlazorItemBankLevelsService { get; set; }
        [Inject] GlobalUserContext GeneralContext { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazGroupService BlazGroupService { get; set; }

        private List<ItemLevelsDto> Items { get; set; } = [];
        private IEnumerable<ItemLevelsDto> CurrentItemLevel { get; set; } = [];
        private NewItemBankDTO Model { get; set; } = new();
        private ItemLevelsDto SelectedLevel { get; set; } = new();
        private List<GetOESGroupDto> SelectedGroups { get; set; } = [];
        private bool IsSubmitButtonDisabled =>
            string.IsNullOrWhiteSpace(Model.Name) ||
            string.IsNullOrWhiteSpace(Model.Code) ||
            string.IsNullOrWhiteSpace(Model.LevelName);
        private ItemBankLevelInsertionWay ItemBankLevelInsertionWay { get; set; }


        protected override async Task OnInitializedAsync()
        {
            // Load available item levels
            Items.AddRange(await BlazorItemBankLevelsService.GetLevels());
        }

        private void OnItemBankLevelInsertionWayChanged(ItemBankLevelInsertionWay itemBankLevelInsertionWay)
        {
            ItemBankLevelInsertionWay = itemBankLevelInsertionWay;

            Model.LevelId = 0;
            Model.LevelName = null;
        }

        private void GetSelectedLevel(ItemLevelsDto itemLevelsDto)
        {
            SelectedLevel = itemLevelsDto;

            if (SelectedLevel != null)
            {
                CurrentItemLevel = [.. Items.Where(x => x.Id == SelectedLevel.Id)];

                Model.LevelId = CurrentItemLevel.FirstOrDefault()?.Id ?? 0;
                Model.LevelName = CurrentItemLevel.FirstOrDefault()?.Name;
            }
            else
            {
                Model.LevelId = 0;
                Model.LevelName = null;
            }
        }

        private async Task<IEnumerable<ItemLevelsDto>> SearchItemsLevelAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Items;
            }

            var itemLevelsDtos = await Task.Run(() =>
                Items.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase)).ToList(),
                cancellationToken
            );

            if (itemLevelsDtos.Count == 0)
            {
                SelectedLevel.Name = value;
                SelectedLevel.Id = 0;
                SelectedLevel.OrganizationSignature = GeneralContext.CurrentOrganizationSignature;
                return default;
            }
            else
            {
                return itemLevelsDtos;
            }
        }

        private async Task OnValidSubmit()
        {
            if (string.IsNullOrWhiteSpace(Model.LevelName))
            {
                Snackbar.Add(Resource.LevelIsRequiredHereForItemBank, Severity.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(Model.Description))
            {
                Snackbar.Add(Resource.RootDescriptionIsRequired, Severity.Error);
                return;
            }

            Model.OESGroupDtos = SelectedGroups ?? [];

            Model.OrganizationId = GeneralContext.CurrentOrganizationId;

            var result = await BlazItemBankService.AddItemBankAsync(Model);

            if (result.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(result.Message, Severity.Success);
                NavigationManager.NavigateTo("/ItemBankList");
            }
            else
            {
                Snackbar.Add(result.Message, Severity.Error);
            }
        }

        private void OnItemBankNameChanged(string value)
        {
            Model.Name = value;
            Model.Code = value?.Trim().Replace(" ", "-");
        }

        private void NavigateBack()
        {
            NavigationManager.NavigateTo("/ItemBankList");
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
    }
}
