using MudBlazor;
using OES.Blazor.Services.Interfaces.AppUser;
using OES.Blazor.Services.Interfaces.GroupsTempleates;
using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Enums;

namespace OES.Blazor.Services.Interfaces.TemplateSubmission
{
    public interface IBlazTemplateSubmissionService
    {
        Task<bool> SubmitTemplateAsync(
            CreateTemplateDto templateModel,
            HashSet<ResourceType> selectedResourceTypes,
            HashSet<string> selectedRoleNames,
            Dictionary<ResourceType, List<string>> rolesPerResource,
            HashSet<Guid> selectedUserIds,
            List<(Guid UserId, string UserName)> availableUsers,
            ISnackbar snackbar,
            IBlazOesRoleTemplateService templateService,
            IBlazUserProfileService userProfileService
        );
    }
}

