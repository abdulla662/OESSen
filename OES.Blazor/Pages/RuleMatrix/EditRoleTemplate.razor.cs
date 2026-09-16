using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.UserDialogs;
using OES.Blazor.Services.Interfaces.AppUser;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.GroupsTempleates;
using OES.Blazor.Services.Interfaces.TemplateSubmission;
using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Dtos.User;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.RuleMatrix
{
    public partial class EditRoleTemplate : ComponentBase
    {
        [Inject] private IBlazOesRoleTemplateService TemplateService { get; set; }
        [Inject] private IBlazUserProfileService UserService { get; set; }
        [Inject] private IBlazTemplateSubmissionService TemplateSubmissionService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IBlazAuthService BlazAuthService { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public Guid TemplateId { get; set; }
        [Parameter] public ResourceType ResourceType { get; set; } = ResourceType.All;

        private CreateTemplateDto model = new();
        private HashSet<ResourceType> selectedResources = [];
        private HashSet<string> selectedRoles = [];
        private Dictionary<ResourceType, List<string>> rolesByResource = [];
        private bool isLoading = true;
        private bool isSubmitting = false;

        // Tree expand/collapse state
        private HashSet<ResourceType> expandedResources = [];
        private HashSet<string> expandedPages = [];

        private List<BlazUserDTO> SelectedUsers = [];
        private HashSet<Guid> SelectedUserIds => [.. SelectedUsers.Select(u => u.ID)];

        protected override async Task OnInitializedAsync()
        {
            await LoadRolesAsync();
            await LoadTemplateAsync();
            await LoadUsersAsync();
            isLoading = false;
        }

        // ==============================
        // Load Roles
        // ==============================
        private async Task LoadRolesAsync()
        {
            var allRoles = await TemplateService.GetAllTemplateRolesAsync();

            if (allRoles == null || allRoles.Count == 0)
            {
                Snackbar.Add(Resource.NoRolesFound, Severity.Info);
                return;
            }

            rolesByResource = ResourceType == ResourceType.All
                ? allRoles.Where(r => r.Key != ResourceType.All).ToDictionary(k => k.Key, v => v.Value)
                : allRoles.Where(r => r.Key == ResourceType).ToDictionary(k => k.Key, v => v.Value);
        }

        private async Task LoadTemplateAsync()
        {
            model = await TemplateService.GetTemplateDetailsAsync(TemplateId);

            if (model == null || model.Id == Guid.Empty)
            {
                Snackbar.Add(Resource.NoData, Severity.Warning);
                return;
            }

            selectedResources = model.ResourcesWithRoles?
                .Select(r => r.ResourceType)
                .Where(r => PageRoleMap.Map.ContainsKey(r))
                .ToHashSet() ?? [];

            selectedRoles = model.ResourcesWithRoles?
                .SelectMany(r => r.Roles)
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

            expandedResources = [.. selectedResources];

            Snackbar.Add(Resource.TemplateLoaded, Severity.Success);
        }

        private async Task LoadUsersAsync()
        {
            if (model != null && model.Id != Guid.Empty)
            {
                var assignedIds = await UserService.GetAssignedUserIdsAsync(model.Id);

                if (assignedIds?.Any() == true)
                {
                    var allUsersDto = await UserService.GetAllAsync();
                    var assignedIdSet = assignedIds.ToHashSet();
                    SelectedUsers = allUsersDto?
                        .Where(u => assignedIdSet.Contains(u.ID))
                        .ToList() ?? [];
                }
            }

            StateHasChanged();
        }

        private void OnResourcesChanged(HashSet<ResourceType> newSelection)
        {
            // Auto-expand newly added resources
            foreach (var r in newSelection.Except(selectedResources))
                expandedResources.Add(r);

            // Collapse removed resources and clean their roles
            foreach (var r in selectedResources.Except(newSelection))
            {
                expandedResources.Remove(r);
                if (PageRoleMap.Map.TryGetValue(r, out var pages))
                {
                    foreach (var page in pages)
                    {
                        expandedPages.Remove(page.PageName);
                        foreach (var role in page.Roles)
                            selectedRoles.Remove(role);
                    }
                }
            }

            selectedResources = newSelection;
            StateHasChanged();
        }

        private void ToggleResource(ResourceType resource)
        {
            if (expandedResources.Contains(resource)) expandedResources.Remove(resource);
            else expandedResources.Add(resource);
            StateHasChanged();
        }

        private void TogglePage(string pageName)
        {
            if (expandedPages.Contains(pageName)) expandedPages.Remove(pageName);
            else expandedPages.Add(pageName);
            StateHasChanged();
        }

        private void OnRoleChecked(string role, bool isChecked)
        {
            if (isChecked) selectedRoles.Add(role);
            else selectedRoles.Remove(role);
            StateHasChanged();
        }

        private void SelectAllRolesForResource(ResourceType resource, List<PageRoleMap.PageRoles> pages)
        {
            foreach (var role in pages.SelectMany(p => p.Roles))
                selectedRoles.Add(role);
            StateHasChanged();
        }

        private void ClearAllRolesForResource(ResourceType resource, List<PageRoleMap.PageRoles> pages)
        {
            foreach (var role in pages.SelectMany(p => p.Roles))
                selectedRoles.Remove(role);
            StateHasChanged();
        }

        private void SelectAllRolesForPage(PageRoleMap.PageRoles page)
        {
            foreach (var role in page.Roles) selectedRoles.Add(role);
            expandedPages.Add(page.PageName);
            StateHasChanged();
        }

        private void ClearAllRolesForPage(PageRoleMap.PageRoles page)
        {
            foreach (var role in page.Roles) selectedRoles.Remove(role);
            StateHasChanged();
        }

        private async Task OnValidSubmitAsync()
        {
            if (isSubmitting) return;
            isSubmitting = true;

            try
            {
                if (selectedResources.Count > 0)
                {
                    var invalidResources = selectedResources
                        .Where(r =>
                            !PageRoleMap.Map.TryGetValue(r, out var pages) ||
                            !pages.SelectMany(p => p.Roles).Any(role => selectedRoles.Contains(role)))
                        .ToList();

                    if (invalidResources.Count > 0)
                    {
                        Snackbar.Add(
                            string.Format(Resource.ResourcesRequireRoles,
                            string.Join(", ", invalidResources)),
                            Severity.Error
                        );

                        return;
                    }
                }

                bool success = await TemplateSubmissionService.SubmitTemplateAsync(
                    model,
                    selectedResources,
                    selectedRoles,
                    rolesByResource,
                    SelectedUserIds,
                    SelectedUsers.ConvertAll(u => (u.ID, u.Name)),
                    Snackbar,
                    TemplateService,
                    UserService
                );

                if (success)
                {
                    await BlazAuthService.RefreshUserContextAsync();
                    MudDialog.Close(DialogResult.Ok(model));
                }

            }
            finally
            {
                isSubmitting = false;
            }
        }

        private void Cancel() => MudDialog.Cancel();

        private async Task OpenUserSelectionDialog()
        {
            var parameters = new DialogParameters
            {
                { nameof(UserSelectionDialog.InitialSelectedUsers), SelectedUsers }
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<UserSelectionDialog>(string.Empty, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is List<BlazUserDTO> selectedUsers)
            {
                SelectedUsers = selectedUsers;
                StateHasChanged();
            }
        }

        private void RemoveUser(BlazUserDTO user)
        {
            SelectedUsers.Remove(user);
            StateHasChanged();
        }
    }
}