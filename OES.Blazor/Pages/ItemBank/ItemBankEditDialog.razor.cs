using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.ItemBank
{
    public partial class ItemBankEditDialog
    {
        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] private IBlazItemBankService BlazItemBankService { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Inject] private IBlazItemBankService BlazorItemBankService { get; set; }

        [Inject] private IBlazGroupService BlazGroupService { get; set; }

        [Inject] IDialogService DialogService { get; set; }

        [Inject] IBlazAuthService AuthService { get; set; }


        [Parameter] public EditItemBankNodeDto EditItemBankNodeDto { get; set; } = new();

        [Parameter] public List<TreeItemResponseDto> Parents { get; set; }

        [Parameter] public List<GetOESGroupDto> OesGroupsDtos { get; set; } = [];


        private IEnumerable<GetOESGroupDto> SelectedGroups { get; set; } = [];

        private bool IsSubmitButtonDisabled =>
           string.IsNullOrWhiteSpace(EditItemBankNodeDto.Name) ||
           string.IsNullOrWhiteSpace(EditItemBankNodeDto.Code);

        private bool _unscored;

        protected override void OnInitialized()
        {
            SelectedGroups = EditItemBankNodeDto?.OESGroupDtos?.ToList() ?? [];
            _unscored = EditItemBankNodeDto?.Unscored ?? false;
            StateHasChanged();
        }

        //private void OnHoursChanged(float value)
        //{
        //    EditItemBankNodeDto.Hours = value;

        //    if (_itemBankValidation.ParentHours > 0)
        //    {
        //        _hoursValid = (value + _itemBankValidation.ChildHours) <= _itemBankValidation.ParentHours;
        //    }
        //    else
        //    {
        //        _hoursValid = true;
        //    }

        //    StateHasChanged();
        //}

        private void OnUnscoredChanged(bool value)
        {
            _unscored = value;
            EditItemBankNodeDto.Unscored = value;
            StateHasChanged();
        }

        private void GetSelectedTreeItem(TreeItemResponseDto treeItem)
        {
            EditItemBankNodeDto.ParentId = treeItem.Id;
        }

        private async Task Submit()
        {
            //OnHoursChanged(EditItemBankNodeDto.Hours);

            //if (!_hoursValid)
            //{
            //    Snackbar.Add(Resource.CannotReduceBelowChildrenHours, Severity.Error);
            //    return;
            //}

            EditItemBankNodeDto.OESGroupDtos = [.. SelectedGroups];

            var response = await BlazorItemBankService.UpdateNode(EditItemBankNodeDto);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(response.Message, Severity.Success);

                MudDialog.Close(DialogResult.Ok(EditItemBankNodeDto));
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private async Task OpenGroupDialog()
        {
            var allowed = await AuthService.IsCurrentUserOwnerAsync<ItemBankGroupsDto>(
                EditItemBankNodeDto.Id,
                BlazorItemBankService.GetItemBankGroupsAsync,
                dto => new List<Guid> { dto.OwnerGroupId ?? Guid.Empty }
            );

            if (!allowed)
            {
                Snackbar.Add(
                    string.Format(Resource.OnlyCreatorCanManageGroups, Resource.ItemBank),
                    Severity.Error
                );
                return;
            }

            SelectedGroups = await DialogInteractionService.OpenSelectionDialogAsync(
                dialogService: DialogService,
                title: Resource.SelectGroup,
                preSelectedItems: SelectedGroups,
                endpointService: async pagination =>
                {
                    var allGroups = await BlazorItemBankService.GetUserItemBankGroupsAsync();

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

            EditItemBankNodeDto.OESGroupDtos = [.. SelectedGroups
                .GroupBy(x => x.Id)
                .Select(g => g.First())];

            StateHasChanged();
        }

        private async Task DeleteGroupAsync(Guid groupId)
        {
            var response = await BlazGroupService.DeleteGroupAsync(groupId);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
                Snackbar.Add(response.Message, Severity.Success);
            else
                Snackbar.Add(response.Message, Severity.Error);
        }

        void Cancel() => MudDialog.Cancel();
    }
}
