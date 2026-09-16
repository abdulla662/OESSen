using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.Question.ItemBankMultiSelectionDialog;
using OES.Blazor.Dialogs.UserDialogs;
using OES.Blazor.Services.Interfaces.AppUser;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.QualityCheckCommittee;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.QualityCheckCommittee;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Dtos.User;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.QualityCheckCommittee
{
    public partial class AddQualityCheckCommittee
    {
        [Inject] IBlazQualityCheckCommitteeService QualityCheckCommitteeService { get; set; } = default!;

        [Inject] IBlazUserProfileService UserProfileService { get; set; } = default!;

        [Inject] NavigationManager Navigation { get; set; } = default!;

        [Inject] ISnackbar Snackbar { get; set; } = default!;

        [Inject] IDialogService DialogService { get; set; } = default!;

        [Inject] IBlazItemBankService BlazItemBankService { get; set; } = default!;

        public List<RootItemBankDto> RootItemBankDtos { get; set; } = [];

        private CreateQualityCheckCommitteeRequestDto Model { get; set; } = new();

        private List<BlazUserDTO> Users { get; set; } = [];

        private RootItemBankDto? SelectedRootItemBankForMultiSelect { get; set; }

        private List<TreeItemResponseDto> SelectedItemBanks { get; set; } = [];

        private Func<RootItemBankDto, string> ItemBankDtoToStringConverter { get; set; } = p => p.Name;

        private bool IsSubmitting { get; set; } = false;

        private bool IsFormInvalidOrSubmitting =>
            IsSubmitting ||
            string.IsNullOrWhiteSpace(Model.Name) ||
            string.IsNullOrWhiteSpace(Model.Description) ||
            Model.ChiefUserId == Guid.Empty ||
            Model.MemberUserIds.Count == 0 ||
            SelectedItemBanks.Count == 0;

        protected override async Task OnInitializedAsync()
        {
            Users = await UserProfileService.GetAllAsync();

            RootItemBankDtos = await BlazItemBankService.GetRootItemBanksNodesAsync();
        }

        private async Task OpenMultiSelectItemBankDialogAsync()
        {
            if (SelectedRootItemBankForMultiSelect == null) return;

            var parameters = new DialogParameters<ItemBankMultiSelectionDialog>
            {
                { p => p.ItemBankRootId, SelectedRootItemBankForMultiSelect.Id },
                { p => p.SelectedItemBankNodesIds, SelectedItemBanks.ConvertAll(b => b.Id)},
                { p => p.EnableCascadingSelection, true }
            };

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

            var dialog = await DialogService.ShowAsync<ItemBankMultiSelectionDialog>(Resource.SelectMultipleItemBanks, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                SelectedItemBanks = (List<TreeItemResponseDto>)result.Data;

                StateHasChanged();
            }
        }

        private async Task<IEnumerable<RootItemBankDto>> SearchItemBankRootsAsync(string value, CancellationToken cancellationToken)
        {
            if (RootItemBankDtos.Count == 0)
                return [];

            if (string.IsNullOrWhiteSpace(value) || (SelectedRootItemBankForMultiSelect?.Name != null && SelectedRootItemBankForMultiSelect.Name.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                return RootItemBankDtos;
            }

            return await FilterListAsync(RootItemBankDtos, ib => ib.Name, value);
        }

        private static async Task<IEnumerable<T>> FilterListAsync<T>(IEnumerable<T> list, Func<T, string> selector, string value)
        {
            if (string.IsNullOrEmpty(value))
                return list;

            await Task.CompletedTask;

            return list.Where(item => selector(item).Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        private void RemoveSelectedItemBank(long id)
        {
            SelectedItemBanks.RemoveAll(b => b.Id == id);
        }

        private async Task OpenChiefSelectionDialog()
        {
            var initialSelected = Model.ChiefUserId != Guid.Empty
                ? Users.Where(u => u.ID == Model.ChiefUserId).ToList()
                : new List<BlazUserDTO>();

            var parameters = new DialogParameters<ChiefSelectionDialog>
            {
                { p => p.InitialSelectedChief, initialSelected }
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<ChiefSelectionDialog>(
                string.Empty,
                parameters,
                options
            );

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is List<BlazUserDTO> selectedUsers && selectedUsers.Count > 0)
            {
                OnChiefUserChanged(selectedUsers[0].ID);
            }
        }

        private void OnChiefUserChanged(Guid newChiefId)
        {
            var oldChiefId = Model.ChiefUserId;

            Model.ChiefUserId = newChiefId;

            if (oldChiefId != Guid.Empty)
            {
                Model.MemberUserIds.Remove(oldChiefId);
            }

            Model.MemberUserIds.Remove(newChiefId);

            if (newChiefId != Guid.Empty)
            {
                Model.MemberUserIds.Insert(0, newChiefId);
            }

            StateHasChanged();
        }

        private async Task OpenMemberSelectionDialog()
        {
            var lockedMembers = Model.ChiefUserId != Guid.Empty
                ? Users.Where(u => u.ID == Model.ChiefUserId).ToList()
                : new List<BlazUserDTO>();

            var initialSelected = Users
                .Where(u => Model.MemberUserIds.Contains(u.ID) && u.ID != Model.ChiefUserId)
                .ToList();

            var parameters = new DialogParameters<MemberSelectionDialog>
            {
                { p => p.InitialSelectedMembers, initialSelected },
                { p => p.LockedMembers, lockedMembers }
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<MemberSelectionDialog>(
                string.Empty,
                parameters,
                options
            );

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is List<BlazUserDTO> selectedUsers)
            {
                Model.MemberUserIds = selectedUsers.ConvertAll(u => u.ID);
                StateHasChanged();
            }
        }

        private void RemoveMember(Guid memberId)
        {
            if (memberId == Model.ChiefUserId) return;

            Model.MemberUserIds.Remove(memberId);

            StateHasChanged();
        }

        private async Task HandleValidSubmit()
        {
            if (Model.ChiefUserId != Guid.Empty && !Model.MemberUserIds.Contains(Model.ChiefUserId))
            {
                Model.MemberUserIds.Add(Model.ChiefUserId);
            }

            Model.SelectedItemBankIds = SelectedItemBanks.ConvertAll(b => b.Id);

            var result = await QualityCheckCommitteeService.CreateCommitteeWithMembersAsync(Model);

            if (result != null && result.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(result.Message, Severity.Success);
                Navigation.NavigateTo("/QualityCheckCommittees");
            }
            else
            {
                Snackbar.Add(result.Message, Severity.Error);
            }
        }

        private void NavigateBack()
        {
            Navigation.NavigateTo("/QualityCheckCommittees");
        }

        private void OnRootItemBankChanged()
        {
            SelectedItemBanks.Clear();

            StateHasChanged();
        }
    }
}
