using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.Question.ItemBankMultiSelectionDialog;
using OES.Blazor.Dialogs.UserDialogs;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.QualityCheckCommittee;
using OES.Blazor.Services.Interfaces.UserService;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.QualityCheckCommittee;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Dtos.User;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.QualityCheckCommittee
{
    public partial class UpdateQualityCheckCommittee : ComponentBase
    {
        [Inject] IBlazQualityCheckCommitteeService QcCommitteeService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] IBlazSessionStorageService SessionStorage { get; set; }

        [Inject] IBlazUserService UserService { get; set; }

        [Inject] private IDialogService DialogService { get; set; } = default!;

        [Inject] private IBlazItemBankService BlazItemBankService { get; set; } = default!;

        private QualityCheckCommitteeDto Model { get; set; } = new();
        private List<BlazUserDTO> Users { get; set; } = [];
        private Func<RootItemBankDto, string> ItemBankDtoToStringConverter { get; set; } = p => p.Name;
        public List<RootItemBankDto> RootItemBankDtos { get; set; } = [];
        private bool IsSubmitButtonDisabled =>
            string.IsNullOrWhiteSpace(Model.Name) ||
            Model.ChiefId == Guid.Empty ||
            Model.Members.Count == 0;

        private List<Guid> _selectedMemberIds = [];
        private RootItemBankDto? _selectedRootItemBankForMultiSelect;
        private List<QualityCheckItemBankDto> _selectedItemBanks = [];


        protected override async Task OnInitializedAsync()
        {
            var id = await SessionStorage.GetValue<long>("PerformEditBtnClick");

            var committeeTask = QcCommitteeService.GetCommitteeByIdAsync(id);

            var usersTask = UserService.GetAll();

            var itemBankRootsTask = BlazItemBankService.GetRootItemBanksNodesAsync();

            await Task.WhenAll(committeeTask, usersTask, itemBankRootsTask);

            var response = await committeeTask;

            var usersResponse = await usersTask;

            RootItemBankDtos = await itemBankRootsTask;

            Users = usersResponse.StatusCode == HttpStatusCode.OK ? usersResponse.Data as List<BlazUserDTO> ?? [] : [];

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Model = response.Data as QualityCheckCommitteeDto ?? new();

                _selectedMemberIds = [.. Model.Members.OrderByDescending(m => m.UserId == Model.ChiefId).Select(m => m.UserId)];

                _selectedItemBanks = [.. Model.ItemBanks];

                if (Model.SelectedRootItemBank != null)
                {
                    _selectedRootItemBankForMultiSelect = RootItemBankDtos.FirstOrDefault(r => r.Id == Model.SelectedRootItemBank.Id);
                }
            }
            else
            {
                Model = new();
            }

        }

        private async Task OpenChiefSelectionDialog()
        {
            var initialSelected = Model.ChiefId != Guid.Empty
                ? [.. Users.Where(u => u.ID == Model.ChiefId)]
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
                OnChiefChanged(selectedUsers[0].ID);
            }
        }

        private void OnChiefChanged(Guid newChiefId)
        {
            var oldChiefId = Model.ChiefId;
            Model.ChiefId = newChiefId;

            _selectedMemberIds = UpdateMemberListForNewChief(oldChiefId, newChiefId);

            UpdateModelMembers();
            StateHasChanged();
        }

        private List<Guid> UpdateMemberListForNewChief(Guid oldChiefId, Guid newChiefId)
        {
            var updatedMemberIds = _selectedMemberIds.ToList();

            if (oldChiefId != Guid.Empty)
            {
                updatedMemberIds.Remove(oldChiefId);
            }

            updatedMemberIds.Remove(newChiefId);

            if (newChiefId != Guid.Empty)
            {
                updatedMemberIds.Insert(0, newChiefId);
            }

            return updatedMemberIds;
        }

        private async Task OpenMemberSelectionDialog()
        {
            var lockedMembers = Model.ChiefId != Guid.Empty
                ? [.. Users.Where(u => u.ID == Model.ChiefId)]
                : new List<BlazUserDTO>();

            var initialSelected = Users
                .Where(u => _selectedMemberIds.Contains(u.ID) && u.ID != Model.ChiefId)
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
                _selectedMemberIds = selectedUsers.ConvertAll(u => u.ID);
                UpdateModelMembers();
                StateHasChanged();
            }
        }

        private void RemoveMember(Guid memberId)
        {
            if (memberId == Model.ChiefId) return;

            _selectedMemberIds.Remove(memberId);
            UpdateModelMembers();
            StateHasChanged();
        }

        private void UpdateModelMembers()
        {
            var oldMembers = new List<QualityCheckCommitteeMemberDto>(Model.Members);

            Model.Members = _selectedMemberIds.ConvertAll(id =>
            {
                var existingMember = oldMembers.FirstOrDefault(m => m.UserId == id);

                if (existingMember != null)
                {
                    return existingMember;
                }

                var user = Users.FirstOrDefault(u => u.ID == id);

                return new QualityCheckCommitteeMemberDto
                {
                    UserId = id,
                    Username = user?.Name,
                    IsActive = true
                };
            });
        }

        private async Task OpenMultiSelectItemBankDialogAsync()
        {
            if (_selectedRootItemBankForMultiSelect == null) return;

            var parameters = new DialogParameters<ItemBankMultiSelectionDialog>
            {
                { p => p.ItemBankRootId, _selectedRootItemBankForMultiSelect.Id },
                { p => p.SelectedItemBankNodesIds, _selectedItemBanks.ConvertAll(b => b.Id)},
                { p => p.EnableCascadingSelection, true }
            };

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

            var dialog = await DialogService.ShowAsync<ItemBankMultiSelectionDialog>(Resource.SelectMultipleItemBanks, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var selectedNodes = (List<TreeItemResponseDto>)result.Data;

                _selectedItemBanks = selectedNodes.ConvertAll(n => new QualityCheckItemBankDto { Id = n.Id, Name = n.Text });

                StateHasChanged();
            }
        }

        private async Task<IEnumerable<RootItemBankDto>> SearchItemBankRootsAsync(string value, CancellationToken cancellationToken)
        {
            if (RootItemBankDtos.Count == 0)
                return [];

            if (string.IsNullOrWhiteSpace(value) || (_selectedRootItemBankForMultiSelect?.Name != null && _selectedRootItemBankForMultiSelect.Name.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                return RootItemBankDtos;
            }

            return await FilterListAsync(RootItemBankDtos, ib => ib.Name, value);
        }

        private void RemoveSelectedItemBank(long id)
        {
            _selectedItemBanks.RemoveAll(b => b.Id == id);
        }

        private static async Task<IEnumerable<T>> FilterListAsync<T>(IEnumerable<T> list, Func<T, string> selector, string value)
        {
            if (string.IsNullOrEmpty(value))
                return list;

            await Task.CompletedTask;

            return list.Where(item => selector(item).Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        public async Task OnValidSubmitAsync()
        {
            Model.ItemBanks = [.. _selectedItemBanks];

            var response = await QcCommitteeService.EditCommitteeAsync(Model);

            if (response == null)
            {
                Snackbar.Add(Resource.AnErrorOccurred, Severity.Error);
                return;
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message ?? Resource.CommitteeUpdatedSuccessfully, Severity.Success);

                NavigationManager.NavigateTo("/QualityCheckCommittees");
            }
            else
            {
                Snackbar.Add(response.Message ?? Resource.CommitteeUpdateFailed, Severity.Error);
            }
        }

        private void Cancel()
        {
            NavigationManager.NavigateTo("/QualityCheckCommittees");
        }

        private void OnRootItemBankChanged()
        {
            _selectedItemBanks.Clear();

            StateHasChanged();
        }
    }
}
