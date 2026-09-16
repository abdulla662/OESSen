using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ILOSection;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Globalization;

namespace OES.Blazor.Pages.ILOTreeView.ILOInsertionDialogBox
{
    public partial class ILOInsertionDialogBox : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] IBlazGroupService BlazGroupService { get; set; }

        [Inject] IDialogService DialogService { get; set; }

        [Inject] IBlazILOService BlazILOService { get; set; }


        [Parameter] public IloInsertionOrUpdateDialogParameters ILOInsertionDialogParams { get; set; } = default!;

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public List<GetOESGroupDto> OesGroupsDtos { get; set; } = [];


        private List<Guid> SelectedGroupsIds { get; set; } = [];

        private List<GetOESGroupDto> SelectedGroups { get; set; } = [];

        private static bool RightToLeft => CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;

        private bool IsSubmitButtonDisabled =>
           string.IsNullOrWhiteSpace(ILOInsertionDialogParams.Name) ||
           string.IsNullOrWhiteSpace(ILOInsertionDialogParams.Code);


        void Submit()
        {
            ILOInsertionDialogParams.GroupsIds = SelectedGroupsIds?.ToList() ?? [];

            MudDialog.Close(DialogResult.Ok(ILOInsertionDialogParams));
        }

        private async Task OpenGroupDialog()
        {
            SelectedGroups = await DialogInteractionService.OpenSelectionDialogAsync(
                dialogService: DialogService,
                title: Resource.SelectGroup,
                preSelectedItems: SelectedGroups,
                endpointService: async pagination =>
                {
                    var allGroups = await BlazILOService.GetUserIloGroupsAsync();

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

            SelectedGroupsIds = SelectedGroups.ConvertAll(g => g.Id);
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
