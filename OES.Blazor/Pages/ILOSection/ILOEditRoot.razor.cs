using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ILOSection;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.ILOSection
{
    public partial class ILOEditRoot : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazILOService BlazILOService { get; set; }
        [Inject] IBlazSessionStorageService SessoinStorage { set; get; }
        [Inject] IBlazGroupService BlazGroupService { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazAuthService AuthService { get; set; }

        private IloEditDTO IloModel { get; set; } = new();
        private IEnumerable<Guid> SelectedGroupsIds { get; set; } = [];
        private List<GetOESGroupDto> OesGroupsDtos { get; set; } = [];
        private IEnumerable<GetOESGroupDto> SelectedGroups { get; set; } = [];
        private bool IsSubmitButtonDisabled =>
           string.IsNullOrWhiteSpace(IloModel.Name) ||
           string.IsNullOrWhiteSpace(IloModel.Code);


        protected override async Task OnInitializedAsync()
        {
            await GetAllGroupsAsync();
        }

        private void OnILONameChanged(string value)
        {
            IloModel.Name = value;
            IloModel.Code = value?.Trim().Replace(" ", "-");
        }

        public async Task GetAllGroupsAsync()
        {
            var id = await SessoinStorage.GetValue<long>("PerformEditBtnClick");

            var response = await BlazILOService.GetILORoot(id);

            var fetchedIloDto = (ILODto)response.Data;

            IloModel.Id = fetchedIloDto.Id;
            IloModel.Name = fetchedIloDto.Name;
            IloModel.Description = fetchedIloDto.Description;
            IloModel.Code = fetchedIloDto.Code;
            SelectedGroupsIds = IloModel.GroupsId = fetchedIloDto.GroupsIds;
            OesGroupsDtos = await BlazILOService.GetUserIloGroupsAsync();
            SelectedGroups = [.. OesGroupsDtos.Where(g => SelectedGroupsIds.Contains(g.Id))];
            StateHasChanged();
        }

        public async Task OnValidSubmitAsync()
        {
            IloModel.GroupsId = [.. SelectedGroupsIds];

            var response = await BlazILOService.EditILORoot(IloModel);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(Resource.ILOUpdatedSuccessfully, Severity.Success);
                NavigationManager.NavigateTo("/ILORoots");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private async Task OpenGroupDialog()
        {
            var allowed = await AuthService.IsCurrentUserOwnerAsync<IloGroupsDto>(
                IloModel.Id,
                BlazILOService.GetIloGroupsAsync,
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
                    var allGroups = await BlazILOService.GetUserIloGroupsAsync();

                    if (!string.IsNullOrWhiteSpace(pagination.SearchKey))
                    {
                        allGroups = [.. allGroups.Where(x => x.Name.Contains(pagination.SearchKey, StringComparison.OrdinalIgnoreCase))];
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

            SelectedGroupsIds = SelectedGroups.Select(g => g.Id).ToList();
        }

        private async Task DeleteGroupAsync(Guid groupId)
        {
            var response = await BlazGroupService.DeleteGroupAsync(groupId);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
                Snackbar.Add(response.Message, Severity.Success);
            else
                Snackbar.Add(response.Message, Severity.Error);
        }

        private void Cancel()
        {
            NavigationManager.NavigateTo("/ILORoots");
        }
    }
}
