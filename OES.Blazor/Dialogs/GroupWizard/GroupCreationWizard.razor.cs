using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.LoadRolesTemplates;
using OES.Blazor.Dialogs.UserDialogs;
using OES.Blazor.Services.Interfaces.AppUser;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.GroupsTempleates;
using OES.Blazor.Services.Interfaces.GroupWizard;
using OES.Blazor.Services.Interfaces.TemplateSubmission;
using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Dtos.User;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Dialogs.GroupWizard
{
    public partial class GroupCreationWizard : ComponentBase
    {
        [Inject] private IBlazOesRoleTemplateService RoleTemplateService { get; set; } = default!;
        [Inject] private IBlazUserProfileService UserService { get; set; } = default!;
        [Inject] private IBlazTemplateSubmissionService TemplateSubmissionService { get; set; } = default!;
        [Inject] private IBlazAuthService BlazAuthService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IGroupWizardResourceService _resourceService { get; set; } = default!;

        [CascadingParameter] private MudDialogInstance? DialogInstance { get; set; }
        [Parameter] required public Guid GroupId { get; set; }
        [Parameter] public EventCallback<string> OnSaved { get; set; }
        [Parameter] public EventCallback OnCanceled { get; set; }

        private int _step = 0;
        private bool _isSaving = false;
        private bool _isLoading = true;
        private bool _isLoadingSelections = false;
        private bool _selectionsLoaded = false;
        private bool _isEditMode => GroupId != Guid.Empty;

        private string _name = string.Empty;
        private string _description = string.Empty;
        private bool _step1Attempted;
        private bool _step2Attempted;

        private HashSet<ResourceType> _selectedResources = [];
        private HashSet<string> _selectedRoles = [];
        private HashSet<ResourceType> _expandedResources = [];
        private HashSet<string> _expandedPages = [];
        private Dictionary<ResourceType, List<string>> _rolesPerResource = [];

        private List<BlazUserDTO> _selectedUsers = [];
        private HashSet<Guid> SelectedUserIds => [.. _selectedUsers.Select(u => u.ID)];
        private Dictionary<ResourceType, List<long>> _selections = [];

        private static (string Label, string Icon, int Index)[] Steps =>
        [
            (Resource.GroupInformation,  Icons.Material.Filled.Info,  0),
            (Resource.ResourcesAndRoles, Icons.Material.Filled.Security,1),
            (Resource.AssignUsers,       Icons.Material.Filled.People,2),
            (Resource.AssignItems,       Icons.Material.Filled.AccountTree,3),
        ];

        #region Life-Cycle

        protected override async Task OnInitializedAsync()
        {
            _isLoading = true;

            var roles = await RoleTemplateService.GetAllTemplateRolesAsync();

            if (roles != null)
                _rolesPerResource = roles.Where(r => r.Key != ResourceType.All).ToDictionary(k => k.Key, v => v.Value);

            if (_isEditMode) await LoadExistingGroupAsync();

            _isLoading = false;
        }

        private async Task LoadExistingGroupAsync()
        {
            var tpl = await RoleTemplateService.GetTemplateDetailsAsync(GroupId);

            if (tpl == null || tpl.Id == Guid.Empty) return;

            _name = tpl.Name;
            _description = tpl.Description ?? string.Empty;
            _selectedResources = [.. tpl.ResourcesWithRoles.Select(r => r.ResourceType).Where(r => PageRoleMap.Map.ContainsKey(r))];
            _selectedRoles = tpl.ResourcesWithRoles.SelectMany(r => r.Roles).ToHashSet(StringComparer.OrdinalIgnoreCase);
            _expandedResources = [.. _selectedResources];

            var assignedIds = await UserService.GetAssignedUserIdsAsync(GroupId);
            if (assignedIds?.Any() == true)
            {
                var idSet = assignedIds.ToHashSet();
                _selectedUsers = (await UserService.GetAllAsync()).Where(u => idSet.Contains(u.ID)).ToList();
            }
        }

        private async Task LoadExistingSelectionsAsync() =>
            await Task.WhenAll(
                _selectedResources
                    .Where(r => !_resourceService.IsConfigOnlyResource(r))
                    .Select(async r => { _selections[r] = await _resourceService.GetAssignedItemIdsAsync(r, GroupId); }));

        #endregion

        #region Navigation

        private void GoBack() { if (_step > 0) _step--; }

        private async Task GoNextAsync()
        {
            if (_step == 0 && !ValidateStep1()) return;
            if (_step == 1 && !ValidateStep2()) return;

            if (_step == 2)
                foreach (var res in _selectedResources.Where(r => !_resourceService.IsConfigOnlyResource(r)))
                    _selections.TryAdd(res, []);

            _step++;

            if (_step == 3 && _isEditMode && !_selectionsLoaded)
            {
                _isLoadingSelections = true;
                StateHasChanged();
                await LoadExistingSelectionsAsync();
                _isLoadingSelections = false;
                _selectionsLoaded = true;
            }

            StateHasChanged();
        }

        private void Cancel()
        {
            if (DialogInstance is not null) DialogInstance.Cancel();
            else _ = OnCanceled.InvokeAsync();
        }

        #endregion

        #region Validation

        private bool ValidateStep1()
        {
            _step1Attempted = true;

            if (!string.IsNullOrWhiteSpace(_name)) return true;

            Snackbar.Add(Resource.GroupNameRequired, Severity.Error);
            return false;
        }

        private bool ValidateStep2()
        {
            _step2Attempted = true;
            if (_selectedResources.Count == 0) { Snackbar.Add(Resource.SelectResourceRequired, Severity.Error); return false; }
            if (_selectedRoles.Count == 0) { Snackbar.Add(Resource.Pleaseselectatleastonerole, Severity.Error); return false; }
            foreach (var res in _selectedResources)
            {
                if (!PageRoleMap.Map.TryGetValue(res, out var pages)) continue;
                if (!pages.Any(p => p.Roles.Any(r => _selectedRoles.Contains(r))))
                {
                    Snackbar.Add(string.Format(Resource.ResourceRequiresRole, res), Severity.Error); return false;
                }
            }
            return true;
        }

        #endregion

        #region Step 2 — Resources & Roles

        private void OnResourcesChanged(HashSet<ResourceType> newSelection)
        {
            foreach (var r in newSelection.Except(_selectedResources)) _expandedResources.Add(r);
            foreach (var r in _selectedResources.Except(newSelection))
            {
                _expandedResources.Remove(r);
                _selections.Remove(r);
                if (!PageRoleMap.Map.TryGetValue(r, out var pages)) continue;
                foreach (var page in pages)
                {
                    _expandedPages.Remove(page.PageName);
                    foreach (var role in page.Roles) _selectedRoles.Remove(role);
                }
            }
            _selectedResources = newSelection;
            StateHasChanged();
        }

        private void ToggleResource(ResourceType r)
        {
            if (!_expandedResources.Add(r))
                _expandedResources.Remove(r);
        }

        private void TogglePage(string name)
        {
            if (!_expandedPages.Add(name))
                _expandedPages.Remove(name);
        }

        private void OnRoleChecked(string role, bool isChecked)
        {
            if (isChecked) _selectedRoles.Add(role); else _selectedRoles.Remove(role);
        }

        private void SelectAllForResource(List<PageRoleMap.PageRoles> pages)
        {
            foreach (var role in pages.SelectMany(p => p.Roles)) _selectedRoles.Add(role);
            StateHasChanged();
        }

        private void ClearAllForResource(List<PageRoleMap.PageRoles> pages)
        {
            foreach (var role in pages.SelectMany(p => p.Roles)) _selectedRoles.Remove(role);
            StateHasChanged();
        }

        private void SelectAllForPage(PageRoleMap.PageRoles page)
        {
            foreach (var role in page.Roles) _selectedRoles.Add(role);
            _expandedPages.Add(page.PageName);
            StateHasChanged();
        }

        private void ClearAllForPage(PageRoleMap.PageRoles page)
        {
            foreach (var role in page.Roles) _selectedRoles.Remove(role);
            StateHasChanged();
        }

        #endregion

        #region Step 3 — Users

        private async Task OpenUserPickerAsync()
        {
            var dialog = await DialogService.ShowAsync<UserSelectionDialog>(
                string.Empty,
                new DialogParameters { { nameof(UserSelectionDialog.InitialSelectedUsers), _selectedUsers } },
                new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Medium, FullWidth = true });

            var result = await dialog.Result;
            if (!result.Canceled && result.Data is List<BlazUserDTO> users)
            {
                _selectedUsers = users;
                StateHasChanged();
            }
        }

        private void RemoveUser(BlazUserDTO user) { _selectedUsers.Remove(user); StateHasChanged(); }

        #endregion

        #region Step 4

        private List<long> GetSelection(ResourceType resource)
        {
            _selections.TryAdd(resource, []);
            return _selections[resource];
        }

        #endregion

        #region Load Template

        private async Task OpenLoadTemplateAsync()
        {
            var dialog = await DialogService.ShowAsync<LoadRolesTemplateDialog>(
                Resource.SelectTemplate,
                new DialogParameters { { DialogParameterKeys.ResourceType, ResourceType.All } },
                new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true });

            var result = await dialog.Result;
            if (!result.Canceled && result.Data is CreateTemplateDto tpl) await ApplyTemplateAsync(tpl);
        }

        private async Task ApplyTemplateAsync(CreateTemplateDto tpl)
        {
            _name = tpl.Name;
            _description = tpl.Description ?? string.Empty;
            _selectedResources = [.. tpl.ResourcesWithRoles.Select(r => r.ResourceType).Where(r => PageRoleMap.Map.ContainsKey(r))];
            _selectedRoles = tpl.ResourcesWithRoles.SelectMany(r => r.Roles).ToHashSet(StringComparer.OrdinalIgnoreCase);
            _expandedResources = [.. _selectedResources];

            var assignedIds = await UserService.GetAssignedUserIdsAsync(tpl.Id);
            if (assignedIds?.Any() == true)
            {
                var idSet = assignedIds.ToHashSet();
                _selectedUsers = (await UserService.GetAllAsync()).Where(u => idSet.Contains(u.ID)).ToList();
            }

            StateHasChanged();
        }

        #endregion

        #region Save

        private async Task SaveAsync(bool isTemplate)
        {
            if (_isSaving) return;
            if (!ValidateStep1() || !ValidateStep2()) return;

            _isSaving = true;
            try
            {
                var model = new CreateTemplateDto
                {
                    Id = GroupId,
                    GroupId = GroupId,
                    Name = _name.Trim(),
                    Description = _description.Trim(),
                    IsTemplate = isTemplate,
                    ResourcesWithRoles = [.. _selectedResources
                        .Select(res => new ResourceWithRolesDto
                        {
                            ResourceType = res,
                            Roles = PageRoleMap.Map.TryGetValue(res, out var pages)
                                ? [.. pages.SelectMany(p => p.Roles).Where(r => _selectedRoles.Contains(r, StringComparer.OrdinalIgnoreCase))]
                                : []
                        })
                        .Where(x => x.Roles.Count > 0)]
                };

                _selectedRoles = model.ResourcesWithRoles.SelectMany(r => r.Roles).ToHashSet(StringComparer.OrdinalIgnoreCase);

                bool success = await TemplateSubmissionService.SubmitTemplateAsync(
                    model, _selectedResources, _selectedRoles, _rolesPerResource,
                    SelectedUserIds, _selectedUsers.ConvertAll(u => (u.ID, u.Name)),
                    Snackbar, RoleTemplateService, UserService);

                if (!success) return;

                var groupId = model.Id;

                if (groupId != Guid.Empty)
                    foreach (var (resource, ids) in _selections.Where(s => s.Value.Count > 0))
                        await _resourceService.AssignGroupToItemsAsync(resource, groupId, ids);

                await BlazAuthService.RefreshUserContextAsync();

                if (DialogInstance is not null) DialogInstance.Close(DialogResult.Ok(true));
                else await OnSaved.InvokeAsync(_name.Trim());
            }
            finally { _isSaving = false; }
        }

        #endregion

        #region UI Helpers

        private static string GetResourceIcon(ResourceType r) => r switch
        {
            ResourceType.Schedule => Icons.Material.Filled.CalendarMonth,
            ResourceType.Papers => Icons.Material.Filled.Assignment,
            ResourceType.Questions => Icons.Material.Filled.Quiz,
            ResourceType.ItemBank => Icons.Material.Filled.AccountTree,
            ResourceType.Ilo => Icons.Material.Filled.Schema,
            _ => Icons.Material.Filled.Category
        };

        #endregion
    }
}