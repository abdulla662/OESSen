
using OES.Helper.Dtos.UserRoles;
using OES.Helper.Enums;
using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Helper.General.GlobalUserContext
{
    public class GlobalUserContext
    {
        // These properties are filled from the SSO JWT:

        public Guid JsonTokenIdentifier { get; set; }

        public Guid UserId { get; set; }

        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public string UserDeviceIpAddress { get; set; }

        public string OrganizationModuleSubscriptionKey { get; set; }

        public long CurrentOrganizationId { get; set; }

        public string CurrentOrganizationSignature { get; set; }

        public AuthenticationType CurrentUserAuthenticationType { get; set; }

        public Guid CurrentModuleKey { get; set; }

        public List<AllowedOrganization> AllowedOrganizations { get; set; } = [];

        public List<UserGroup> SsoUserGroups { get; set; } = [];

        public List<UserRole> SsoUserRoles { get; set; } = [];

        public List<RoleDto> OesUserRoles { get; set; } = [];

        public List<UserGroupsAndRolesDto> OesUserGroupsAndRoles { get; set; } = [];

        public List<PageRoleDTO> UserAccessiblePages { get; set; } = [];

        public bool HasAIFeaturesAccess { get; set; }
    }
}
