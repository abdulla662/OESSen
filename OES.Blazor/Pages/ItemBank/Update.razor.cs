using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Helper.Dtos.ItemBank.API;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.ItemBank
{
    public partial class Update : ComponentBase
    {
        [Inject] IBlazItemBankService ItemBankService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazSessionStorageService SessoinStorage { set; get; }
        [Inject] IBlazGroupService BlazGroupService { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazAuthService AuthService { get; set; }


        public ApiResponse Response { get; set; }

        private NewItemBankDTO Model { get; set; } = new();

        private IEnumerable<GetOESGroupDto> SelectedGroups { get; set; } = [];

        private List<GetOESGroupDto> OesGroupsDtos { get; set; } = [];

        private bool IsSubmitButtonDisabled =>
          string.IsNullOrWhiteSpace(Model.Name) ||
          string.IsNullOrWhiteSpace(Model.Code);


        protected override async Task OnInitializedAsync()
        {
            try
            {
                var id = await SessoinStorage.GetValue<long>("PerformEditBtnClick");

                Model = await ItemBankService.GetItemBankByID(id);

                OesGroupsDtos = await BlazGroupService.GetGroupsAsync();

                var modelGroupIds = Model.OESGroupDtos?.Select(a => a.Id) ?? [];

                SelectedGroups = OesGroupsDtos.IntersectBy(modelGroupIds, x => x.Id);
            }
            finally
            {
                StateHasChanged();
            }
        }

        public async Task OnValidSubmitAsync()
        {
            Model.OESGroupDtos = [.. SelectedGroups];

            Response = await ItemBankService.UpdateItemBank(Model);

            StateHasChanged();

            if (Response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(Response.Message, Severity.Success);

                NavigationManager.NavigateTo("/ItemBankList");
            }
            else
            {
                Snackbar.Add(Response.Message, Severity.Error);
            }
        }

        private async Task OpenGroupDialog()
        {
            var allowed = await AuthService.IsCurrentUserOwnerAsync(
                Model.Id,
                ItemBankService.GetItemBankGroupsAsync,
                dto => [dto.OwnerGroupId ?? Guid.Empty]
            );

            if (!allowed)
            {
                Snackbar.Add(string.Format(Resource.OnlyCreatorCanManageGroups, Resource.ItemBank), Severity.Error);
                return;
            }

            var result = await DialogInteractionService.OpenSelectionDialogAsync(
                dialogService: DialogService,
                title: Resource.SelectGroup,
                preSelectedItems: SelectedGroups,
                endpointService: async pagination =>
                {
                    var allGroups = await ItemBankService.GetUserItemBankGroupsAsync();

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
                resourceType: ResourceType.ItemBank,
                onDelete: EventCallback.Factory.Create<Guid>(this, DeleteGroupAsync)
            );

            SelectedGroups = [.. result
                .GroupBy(x => x.Id)
                .Select(g => g.First())];
        }

        private async Task DeleteGroupAsync(Guid groupId)
        {
            var response = await BlazGroupService.DeleteGroupAsync(groupId);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
                Snackbar.Add(response.Message, Severity.Success);
            else
                Snackbar.Add(response.Message, Severity.Error);
        }

        private void OnItemBankNameChanged(string value)
        {
            Model.Name = value;
            Model.Code = value?.Trim().Replace(" ", "-");
        }

        private void Cancel()
        {
            NavigationManager.NavigateTo("/ItemBankList");
        }
    }
}
