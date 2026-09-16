using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Pages.ILOTreeView.ILOInsertionDialogBox;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ILOSection;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.ILOTreeView.ILOEditDialog
{
    public partial class ILOEditDialog
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazILOService BlazIloService { get; set; }
        [Inject] IBlazGroupService BlazGroupService { get; set; }
        [Inject] IBlazAuthService AuthService { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public IloInsertionOrUpdateDialogParameters IloInsertionOrUpdateDialogParams { get; set; } = new();
        [Parameter] public List<TreeItemResponseDto> Parents { get; set; }
        [Parameter] public List<GetOESGroupDto> OesGroupsDtos { get; set; } = [];

        private List<Guid> SelectedGroupsIds = [];
        private List<GetOESGroupDto> SelectedGroups = [];
        private static bool RightToLeft =>
            System.Globalization.CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;

        private bool IsSubmitButtonDisabled =>
            string.IsNullOrWhiteSpace(IloInsertionOrUpdateDialogParams.Name) ||
            string.IsNullOrWhiteSpace(IloInsertionOrUpdateDialogParams.Code);


        protected override void OnInitialized()
        {
            IloInsertionOrUpdateDialogParams.GroupsIds ??= [];

            SelectedGroups = [.. OesGroupsDtos.Where(g => IloInsertionOrUpdateDialogParams.GroupsIds.Contains(g.Id))];

            SelectedGroupsIds = SelectedGroups.ConvertAll(x => x.Id);
        }

        private void GetSelectedTreeItem(TreeItemResponseDto item)
        {
            IloInsertionOrUpdateDialogParams.ParentId = item.Id;
        }

        private async Task OpenGroupDialog()
        {
            var allowed = await AuthService.IsCurrentUserOwnerAsync<IloGroupsDto>(
                IloInsertionOrUpdateDialogParams.Id,
                BlazIloService.GetIloGroupsAsync,
                dto => new List<Guid> { dto.OwnerGroupId ?? Guid.Empty }
            );

            if (!allowed)
            {
                Snackbar.Add(
                    string.Format(Resource.OnlyCreatorCanManageGroups, Resource.ILO),
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
                    var allGroups = await BlazIloService.GetUserIloGroupsAsync();

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
                resourceType: ResourceType.Ilo,
                onDelete: EventCallback.Factory.Create<Guid>(this, DeleteGroupAsync)
            );

            SelectedGroups = [.. SelectedGroups
                .GroupBy(x => x.Id)
                .Select(g => g.First())];

            SelectedGroupsIds = SelectedGroups.ConvertAll(g => g.Id);

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

        private void Submit()
        {
            IloInsertionOrUpdateDialogParams.GroupsIds = [.. SelectedGroupsIds];

            MudDialog.Close(DialogResult.Ok(IloInsertionOrUpdateDialogParams));
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
