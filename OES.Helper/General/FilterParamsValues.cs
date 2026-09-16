using OES.Helper.Dtos.UserRoles;
using OES.Helper.General.GlobalUserContext;

namespace OES.Helper.General
{
    public class FilterParamsValues
    {
        public bool Authorize { get; set; }

        public bool ShowDeleted { get; set; }

        public bool IsActive { get; set; }

        public bool ApplyFilter { get; set; }

        public bool ApplyShowDeletedFilter { get; set; }

        public bool ApplyIsActiveFilter { get; set; }

        public bool ApplyOrganizationIdFilter { get; set; }

        public bool ApplySignatureFilter { get; set; }

        public List<UserRole> SsoUserRoles { get; set; } = [];

        public string Signature { get; set; }

        public long OrganizationId { get; set; }

        public string UserId { get; set; }

        public string UserEmail { get; set; }

        public bool BodyEncrypted { get; set; } = true;

        public Guid ModuleId { get; set; }

        public string CurrentBearerToken { get; set; }

        public List<UserGroupsAndRolesDto> OesUserGroupsAndRoles { get; set; } = new();
    }
}
