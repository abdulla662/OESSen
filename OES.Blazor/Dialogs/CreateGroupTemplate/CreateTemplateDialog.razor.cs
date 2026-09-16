using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OES.Blazor.Dialogs.LoadRolesTemplates;
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

namespace OES.Blazor.Dialogs.CreateGroupTemplate
{
    public partial class CreateTemplateDialog
    {
        // ==============================
        // Injected Services
        // ==============================
        [Inject] IBlazOesRoleTemplateService RoleTemplateService { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager Navigator { get; set; }
        [Inject] IBlazUserProfileService UserService { get; set; }
        [Inject] IBlazTemplateSubmissionService TemplateSubmissionService { get; set; }
        [Inject] private IBlazAuthService BlazAuthService { get; set; }

        // ==============================
        // Parameters
        // ==============================
        [CascadingParameter] MudDialogInstance DialogInstance { get; set; } = default!;
        [Parameter] public ResourceType TypeOfResource { get; set; } = ResourceType.All;

        // ==============================
        // Internal State
        // ==============================
        private CreateTemplateDto TemplateModel = new();
        private EditContext TemplateEditContext;

        private Dictionary<ResourceType, List<string>> RolesPerResource = [];
        private HashSet<ResourceType> SelectedResourceTypes = [];
        private HashSet<string> SelectedRoleNames = [];
        private HashSet<ResourceType> ExpandedResources = [];
        private HashSet<string> ExpandedPages = [];

        private List<BlazUserDTO> SelectedUsers = [];
        private HashSet<Guid> SelectedUserIds => [.. SelectedUsers.Select(u => u.ID)];

        private bool IsSubmitting = false;
        private bool SubmitAttempted = false;

        // ==============================
        // Initialization
        // ==============================
        protected override async Task OnInitializedAsync()
        {
            TemplateEditContext = new EditContext(TemplateModel);
            TemplateModel.ResourceType = TypeOfResource;

            await LoadRolesAsync();
        }

        // ==============================
        // Load Roles
        // ==============================
        private async Task LoadRolesAsync()
        {
            var fetchedRoles = await RoleTemplateService.GetAllTemplateRolesAsync();

            if (fetchedRoles == null || fetchedRoles.Count == 0)
            {
                return;
            }

            RolesPerResource = TypeOfResource == ResourceType.All
                ? fetchedRoles.Where(r => r.Key != ResourceType.All).ToDictionary(k => k.Key, v => v.Value)
                : fetchedRoles.Where(r => r.Key == TypeOfResource).ToDictionary(k => k.Key, v => v.Value);
        }

        // ==============================
        // Resource Selection Changed
        // ==============================
        private void OnResourcesChanged(HashSet<ResourceType> selectedTypes)
        {
            foreach (var r in selectedTypes.Except(SelectedResourceTypes))
                ExpandedResources.Add(r);

            foreach (var r in SelectedResourceTypes.Except(selectedTypes))
            {
                ExpandedResources.Remove(r);
                if (PageRoleMap.Map.TryGetValue(r, out var pages))
                {
                    foreach (var page in pages)
                    {
                        ExpandedPages.Remove(page.PageName);
                        foreach (var role in page.Roles)
                            SelectedRoleNames.Remove(role);
                    }
                }
            }

            SelectedResourceTypes = selectedTypes;
            StateHasChanged();
        }

        private void ToggleResource(ResourceType resource)
        {
            if (ExpandedResources.Contains(resource)) ExpandedResources.Remove(resource);
            else ExpandedResources.Add(resource);
            StateHasChanged();
        }

        private void TogglePage(string pageName)
        {
            if (ExpandedPages.Contains(pageName)) ExpandedPages.Remove(pageName);
            else ExpandedPages.Add(pageName);
            StateHasChanged();
        }

        private void OnRoleChecked(string role, bool isChecked)
        {
            if (isChecked) SelectedRoleNames.Add(role);
            else SelectedRoleNames.Remove(role);
            StateHasChanged();
        }

        private void SelectAllRolesForResource(ResourceType resource, List<PageRoleMap.PageRoles> pages)
        {
            foreach (var role in pages.SelectMany(p => p.Roles))
                SelectedRoleNames.Add(role);
            StateHasChanged();
        }

        private void ClearAllRolesForResource(ResourceType resource, List<PageRoleMap.PageRoles> pages)
        {
            foreach (var role in pages.SelectMany(p => p.Roles))
                SelectedRoleNames.Remove(role);
            StateHasChanged();
        }

        private void SelectAllRolesForPage(PageRoleMap.PageRoles page)
        {
            foreach (var role in page.Roles) SelectedRoleNames.Add(role);
            ExpandedPages.Add(page.PageName);
            StateHasChanged();
        }

        private void ClearAllRolesForPage(PageRoleMap.PageRoles page)
        {
            foreach (var role in page.Roles) SelectedRoleNames.Remove(role);
            StateHasChanged();
        }

        // ==============================
        // Role Selection Changed
        // ==============================
        private void OnRolesChanged(HashSet<string> roles)
        {
            SelectedRoleNames = roles;
            StateHasChanged();
        }

        // ==============================
        // Save as Group
        // ==============================
        private async Task SaveGroup()
        {
            if (IsSubmitting)
                return;

            IsSubmitting = true;
            SubmitAttempted = true;
            TemplateModel.IsTemplate = false;

            BuildResourcesWithRoles();

            if (!ValidateResources())
            {
                IsSubmitting = false;
                return;
            }

            bool success = await SubmitTemplateAsync();

            if (success)
            {
                await BlazAuthService.RefreshUserContextAsync();

                DialogInstance.Close(DialogResult.Ok(TemplateModel));
            }

            IsSubmitting = false;
        }

        // ==============================
        // Save As Template
        // ==============================
        private async Task SaveAsTemplate()
        {
            if (IsSubmitting)
                return;

            IsSubmitting = true;
            TemplateModel.IsTemplate = true;

            BuildResourcesWithRoles();

            if (!ValidateResources())
            {
                IsSubmitting = false;
                return;
            }

            bool success = await SubmitTemplateAsync();

            if (success)
                DialogInstance.Close(DialogResult.Ok(TemplateModel));

            IsSubmitting = false;
        }

        // ==============================
        // Build Resource + Roles Mapping
        // ==============================
        private void BuildResourcesWithRoles()
        {
            foreach (var resource in SelectedResourceTypes)
            {
                if (PageRoleMap.Map.TryGetValue(resource, out var pages))
                    RolesPerResource[resource] = [.. pages.SelectMany(p => p.Roles)];
            }

            TemplateModel.ResourcesWithRoles = [.. SelectedResourceTypes
            .Select(resource => new ResourceWithRolesDto
            {
                ResourceType = resource,
                Roles = RolesPerResource.TryGetValue(resource, out var roles)
                    ? [.. roles.Where(role => SelectedRoleNames.Contains(role))]
                    : []
            })];
        }

        // ==============================
        // Validate Required Roles
        // ==============================
        private bool ValidateResources()
        {
            var resourceWithoutRoles = TemplateModel
                .ResourcesWithRoles
                .FirstOrDefault(r => r.Roles.Count == 0);

            if (resourceWithoutRoles != null)
            {
                Snackbar.Add(
                    string.Format(
                        Resource.ResourcesRequireRoles,
                        resourceWithoutRoles.ResourceType),
                    Severity.Error);

                return false;
            }

            return true;
        }

        // ==============================
        // Submit Template
        // ==============================
        private Task<bool> SubmitTemplateAsync() =>
            TemplateSubmissionService.SubmitTemplateAsync(
                TemplateModel,
                SelectedResourceTypes,
                SelectedRoleNames,
                RolesPerResource,
                SelectedUserIds,
                SelectedUsers.ConvertAll(u => (u.ID, u.Name)),
                Snackbar,
                RoleTemplateService,
                UserService
            );

        // ==============================
        // Cancel Dialog
        // ==============================
        private void Cancel() => DialogInstance.Cancel();

        // ==============================
        // User Selection Dialog
        // ==============================
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

            var dialog = await DialogService.ShowAsync<UserSelectionDialog>(
                string.Empty,
                parameters,
                options
            );

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is List<BlazUserDTO> selectedUsers)
            {
                SelectedUsers = selectedUsers;
                StateHasChanged();
            }
        }

        // ==============================
        // Display Selected Users
        // ==============================
        private void RemoveUser(BlazUserDTO user)
        {
            SelectedUsers.Remove(user);
            StateHasChanged();
        }

        // ==============================
        // Load Saved Templates Dialog
        // ==============================
        private async Task OpenSavedTemplatesDialog()
        {
            var parameters = new DialogParameters
            {
                { DialogParameterKeys.ResourceType, TypeOfResource }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<LoadRolesTemplateDialog>(
                Resource.SelectTemplate,
                parameters,
                options
            );

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is CreateTemplateDto selectedTemplate)
            {
                await ApplyTemplateSelection(selectedTemplate);
            }
        }

        // ==============================
        // Apply Loaded Template
        // ==============================
        private async Task ApplyTemplateSelection(CreateTemplateDto selectedTemplate)
        {
            var assignedUserIds = await UserService.GetAssignedUserIdsAsync(selectedTemplate.Id);

            TemplateModel = selectedTemplate;
            TemplateModel.Id = Guid.Empty;
            TemplateModel.GroupId = Guid.Empty;
            TemplateModel.IsTemplate = false;

            SelectedResourceTypes = [.. TemplateModel.ResourcesWithRoles
                .Select(r => r.ResourceType)
                .Where(r => PageRoleMap.Map.ContainsKey(r))];

            SelectedRoleNames = TemplateModel.ResourcesWithRoles
                .SelectMany(r => r.Roles)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (assignedUserIds.Any())
            {
                var allUsers = await UserService.GetAllAsync();
                var assignedUserIdSet = assignedUserIds.ToHashSet();
                SelectedUsers = allUsers.Where(u => assignedUserIdSet.Contains(u.ID)).ToList();
            }
            else
            {
                SelectedUsers = [];
            }

            StateHasChanged();
        }
    }
}
