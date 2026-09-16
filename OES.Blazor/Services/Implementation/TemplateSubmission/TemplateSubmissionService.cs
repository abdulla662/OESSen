using MudBlazor;
using OES.Blazor.Services.Interfaces.AppUser;
using OES.Blazor.Services.Interfaces.GroupsTempleates;
using OES.Blazor.Services.Interfaces.TemplateSubmission;
using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Services.Implementation.TemplateSubmission
{
    public class TemplateSubmissionService : IBlazTemplateSubmissionService
    {
        private static readonly string MessageEnterName = Resource.TemplateNameRequired;
        private static readonly string MessageSelectResource = Resource.SelectResourceRequired;
        private static readonly string MessageSelectRole = Resource.Pleaseselectatleastonerole;
        private static readonly string MessageOperationFail = Resource.TemplateOperationFailed;
        private static readonly string MessageAssignUsersSuccess = Resource.AssignUsersSuccess;
        private static readonly string MessageUnassignAll = Resource.UnassignAllSuccess;
        private static readonly string MessageAssignFail = Resource.AssignUsersFailed;

        public async Task<bool> SubmitTemplateAsync(
            CreateTemplateDto templateModel,
            HashSet<ResourceType> selectedResourceTypes,
            HashSet<string> selectedRoleNames,
            Dictionary<ResourceType, List<string>> rolesPerResource,
            HashSet<Guid> selectedUserIds,
            List<(Guid UserId, string UserName)> availableUsers,
            ISnackbar snackbar,
            IBlazOesRoleTemplateService templateService,
            IBlazUserProfileService userProfileService
        )
        {
            if (string.IsNullOrWhiteSpace(templateModel.Name))
            {
                snackbar.Add(MessageEnterName, Severity.Error);
                return false;
            }

            if (selectedResourceTypes.Count == 0)
            {
                snackbar.Add(MessageSelectResource, Severity.Error);
                return false;
            }

            if (selectedRoleNames.Count == 0)
            {
                snackbar.Add(MessageSelectRole, Severity.Error);
                return false;
            }

            var allValidRoles = PageRoleMap.Map.Values
                .SelectMany(pages => pages.SelectMany(p => p.Roles))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!selectedRoleNames.All(allValidRoles.Contains))
            {
                snackbar.Add(Resource.InvalidRolesSelected, Severity.Error);
                return false;
            }

            templateModel.ResourcesWithRoles = [.. selectedResourceTypes
                .Select(resourceType => new ResourceWithRolesDto
                {
                    ResourceType = resourceType,
                    Roles = PageRoleMap.Map.TryGetValue(resourceType, out var pages)
                        ? [.. pages.SelectMany(p => p.Roles).Where(selectedRoleNames.Contains)]
                        : []
                })
                .Where(x => x.Roles.Count > 0)];

            if (selectedUserIds.Count > 0)
            {
                templateModel.UsersAssignments = [.. selectedResourceTypes
                    .SelectMany(resourceType =>
                    {
                        var rolesForResource = PageRoleMap.Map.TryGetValue(resourceType, out var pages)
                            ? pages.SelectMany(p => p.Roles).Where(selectedRoleNames.Contains).ToList()
                            : [];

                        if (rolesForResource.Count == 0)
                        {
                            return [];
                        }

                        return selectedUserIds.Select(userId => new UserResourceRoleDto
                        {
                            UserId = userId,
                            UserName = availableUsers.FirstOrDefault(u => u.UserId == userId).UserName ?? string.Empty,
                            ResourceType = resourceType,
                            Roles = rolesForResource
                        });
                    })];
            }

            bool isNewTemplate = templateModel.Id == Guid.Empty;

            var response = isNewTemplate
                ? await templateService.CreateTemplateAsync(templateModel)
                : await templateService.UpdateTemplateAsync(templateModel);

            if (response?.CustomCodeStatus != CustomCodeStatus.Success)
            {
                snackbar.Add(response?.Message ?? MessageOperationFail, Severity.Error);
                return false;
            }

            Guid groupId = templateModel.Id;

            if (isNewTemplate)
            {
                if (response.Data is Guid guidValue)
                {
                    groupId = guidValue;
                }
                else
                {
                    var stringId = response.Data?.ToString();
                    if (!Guid.TryParse(stringId, out groupId))
                    {
                        var dataType = response.Data?.GetType();
                        var idProp = dataType?.GetProperty("Id");

                        if (idProp != null)
                        {
                            var idValue = idProp.GetValue(response.Data);
                            _ = Guid.TryParse(idValue?.ToString(), out groupId);
                        }
                    }
                }

                if (groupId == Guid.Empty)
                {
                    snackbar.Add(Resource.TemplateOperationFailed, Severity.Error);
                    return false;
                }
            }

            templateModel.Id = groupId;
            templateModel.GroupId = groupId;

            var groupAssignmentRequest = new UsersToGroupDto
            {
                GroupId = groupId,
                UsersIds = [.. selectedUserIds]
            };

            var userAssignmentResponse = await userProfileService.AssignAndUnassignUserToGroupAsync(groupAssignmentRequest);

            if (userAssignmentResponse?.CustomCodeStatus == CustomCodeStatus.Success)
            {
                if (selectedUserIds.Count > 0)
                {
                    snackbar.Add(MessageAssignUsersSuccess, Severity.Success);
                }
            }
            else
            {
                snackbar.Add(userAssignmentResponse?.Message ?? MessageAssignFail, Severity.Warning);
            }

            return true;
        }
    }
}
