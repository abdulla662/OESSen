using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ILOSection;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.ILOSection
{
    public partial class NewRoot : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IBlazILOService BlazILOService { get; set; }
        [Inject] IBlazGroupService BlazGroupService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IDialogService DialogService { get; set; }

        private ILONewRootDto Model { get; set; } = new ILONewRootDto();
        private IEnumerable<Guid> SelectedGroupsIds { get; set; } = [];
        private List<GetOESGroupDto> OesGroupsDtos { get; set; } = [];
        private List<GetOESGroupDto> SelectedGroups { get; set; } = [];

        private bool IsSubmitButtonDisabled =>
           string.IsNullOrWhiteSpace(Model.Name) ||
           string.IsNullOrWhiteSpace(Model.Code);

        protected override async Task OnInitializedAsync()
        {
            OesGroupsDtos.AddRange(await BlazGroupService.GetGroupsAsync() ?? []);
        }

        private void OnILONameChanged(string value)
        {
            Model.Name = value;
            Model.Code = value?.Trim().Replace(" ", "-");
        }

        public async Task OnValidSubmitAsync()
        {
            Model.GroupsId = SelectedGroups?.Select(g => g.Id).ToList() ?? [];

            var response = await BlazILOService.AddRoot(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message ?? Resource.ILOAddedSuccessfully, Severity.Success);
                NavigationManager.NavigateTo("/ILORoots");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void Cancel()
        {
            NavigationManager.NavigateTo("/ILORoots");
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
    }
}
